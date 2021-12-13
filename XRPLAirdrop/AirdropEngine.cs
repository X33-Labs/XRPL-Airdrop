using Newtonsoft.Json;
using RippleDotNet;
using RippleDotNet.Model.Account;
using RippleDotNet.Model.Server;
using RippleDotNet.Model.Transaction;
using RippleDotNet.Responses.Transaction.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using XRPLAirdrop.db.models;
using XRPLAirdrop.Models;

namespace XRPLAirdrop
{
    public class AirdropEngine
    {
        private static database db;
        private static Settings config;
        private static ConsoleScreen screen;
        private static Spinner spinner;
        private static XRPL xrpl;

        public AirdropEngine(Settings _config, Spinner _spinner)
        {
            config = _config;
            db = new database();
            screen = new ConsoleScreen(config);
            spinner = _spinner;
            xrpl = new XRPL(config);
        }
        public async Task SendAirDropAsync()
        {
            screen.ClearConsoleLines();
            screen.InitScreen(ref spinner, " Processing Airdrop...");

            try
            {
                if (config.excludeBots)
                {
                    db.UpdateExclusionRecordsInAirdrop();
                }
                if (config.excludeIfUserHasABalance)
                {
                    db.UpdateFailureAirdrop("Excluded from airdrop. account already has a balance", config);
                }
                if (config.includeOnlyIfHolder)
                {
                    db.UpdateFailureAirdrop("Excluded from airdrop. account not holding enough of a balance", config);
                }
                if (config.xrplVerifyEnabled)
                {
                    db.UpdateFailureAirdrop("Excluded from airdrop. address was not verified through xrplverify.com", config);
                }

                int MaxDropAmt = await xrpl.ReturnAmountToDrop();
                if (MaxDropAmt < config.numberOfTrustlines)
                {
                    config.numberOfTrustlines = MaxDropAmt;
                }

                Queue<Airdrop> airDropList = db.GetNonDroppedRecord(config);
                int countSuccess = 0;
                int countFailed = 0;
                int totalQueued = airDropList.Count;
                IRippleClient client = new RippleClient(config.websockUrl);
                client.Connect();
                AccountInfo accountInfo = await client.AccountInfo(config.airdropAddress);
                uint sequence = accountInfo.AccountData.Sequence;

                int toBeVerifiedCount = 0;

                //Get Current Fees
                Fee f = await client.Fees();

                do
                {
                    Airdrop a = airDropList.Peek();
                    try
                    {
                        while (Convert.ToInt32(Math.Floor(f.Drops.OpenLedgerFee * config.feeMultiplier)) > config.maximumFee)
                        {
                            screen.ClearConsoleLines(28);
                            screen.WriteMessages("Waiting...fees too high. Current Open Ledger Fee: " + f.Drops.OpenLedgerFee, "Fees configured based on fee multiplier: " + Convert.ToInt32(Math.Floor(f.Drops.MedianFee * config.feeMultiplier)));
                            Thread.Sleep(config.accountLinesThrottle * 1000);
                            //Get Current Fees
                            f = await client.Fees();
                        }

                        int feeInDrops = Convert.ToInt32(Math.Floor(f.Drops.OpenLedgerFee * config.feeMultiplier));

                        Submit response = await xrpl.SendXRPPaymentAsync(client, a.address, sequence, feeInDrops);
                        //Transaction Node isn't Current. Wait for Network
                        if (response.EngineResult == "noCurrent" || response.EngineResult == "noNetwork")
                        {
                            int retry = 0;
                            while ((response.EngineResult == "noCurrent" || response.EngineResult == "noNetwork") && retry < 3)
                            {
                                //Throttle for node to catch up
                                Thread.Sleep(config.txnThrottle * 3000);
                                response = await xrpl.SendXRPPaymentAsync(client, a.address, sequence, feeInDrops);
                                retry++;
                            }
                        }
                        else if (response.EngineResult == "telCAN_NOT_QUEUE_FEE")
                        {
                            sequence++;
                            //Throttle, check fees and try again
                            Thread.Sleep(config.txnThrottle * 3000);
                        }
                        else if (response.EngineResult == "tesSUCCESS" || response.EngineResult == "terQUEUED")
                        {
                            //Transaction Accepted by node successfully. Verify later.
                            countSuccess++;
                            db.UpdateAirdropRecord(a.address, 1, 0, response);
                            sequence++;
                            toBeVerifiedCount++;
                            airDropList.Dequeue();
                        }
                        else if (response.EngineResult == "tecPATH_DRY" || response.EngineResult == "tecDST_TAG_NEEDED")
                        {
                            //Trustline was removed or Destination Tag needed for address
                            countSuccess++;
                            db.UpdateAirdropRecord(a.address, 1, 1, response);
                            sequence++;
                            airDropList.Dequeue();
                        }
                        else
                        {
                            //Failed
                            countFailed++;
                            db.UpdateFailureAirdrop(a.address, response);
                            sequence++;
                            airDropList.Dequeue();
                        }
                    }
                    catch (Exception ex)
                    {
                        //Failed
                        countFailed++;
                        db.UpdateFailureAirdropException(a.address, ex.Message);
                        airDropList.Dequeue();
                    }
                    Thread.Sleep(config.txnThrottle * 1000);
                    totalQueued--;
                    screen.ClearConsoleLines(28);
                    screen.WriteMessages("Queued Accounts: " + totalQueued, "Successful Transactions: " + countSuccess + "   Failed Transactions: " + countFailed);

                    //Verify once Txn count gets to 10
                    if (toBeVerifiedCount > 9)
                    {
                        //Verify Past Transactions
                        await VerifyTransactions(client);
                        toBeVerifiedCount = 0;
                        //Update Fee calculation after verification
                        f = await client.Fees();
                        screen.ClearConsoleLines();
                        screen.InitScreen(" Processing Airdrop...");
                    }

                } while (airDropList.Count > 0);

                //Check to see if there's any pending Verifications
                await VerifyTransactions(client);

                client.Disconnect();
                spinner.Stop();
                screen.ClearConsoleLines();
                screen.WriteMessages("Airdrop Finished!", "Successful Transactions: " + countSuccess + "   Failed Transactions: " + countFailed, "Press any key to go back to the menu...");
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                spinner.Stop();
                screen.ClearConsoleLines();
                Console.SetCursorPosition(0, 27);
                Console.WriteLine("Error: " + ex.Message);
                Console.ReadLine();
            }

        }

        private static async Task<int> VerifyTransactions(IRippleClient client)
        {
            Queue<Airdrop> unVerifiedList = db.GetUnverifiedRecords(config);
            int retries = 0;
            try
            {
                screen.ClearConsoleLines();
                screen.WriteMessages("Verifying Transactions... ");
                //Wait a few seconds for ledger finality
                Thread.Sleep(3000);
                if (unVerifiedList.Count > 0)
                {
                    do
                    {
                        Airdrop a = unVerifiedList.Peek();
                        ITransactionResponseCommon response = await client.Transaction(a.txn_hash);
                        Thread.Sleep(500);
                        if (response.Validated != null)
                        {
                            if (response.Validated.Value)
                            {
                                //Validated
                                db.UpdateAirdropRecord(a.address, 1, 1, null);
                                unVerifiedList.Dequeue();
                                retries = 0;
                            }
                            else
                            {
                                //Could not validate
                                retries++;
                                Thread.Sleep(config.txnThrottle * 1000);
                            }
                        }
                        else
                        {
                            //Could not validate
                            retries++;
                            Thread.Sleep(config.txnThrottle * 1000);
                        }

                        if (retries >= 3)
                        {
                            db.UpdateAirdropRecord(a.address, 1, -2, null);
                            unVerifiedList.Dequeue();
                        }
                    } while (unVerifiedList.Count > 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return 0;
        }

        public async Task VerifyPendingFailed()
        {
            screen.ClearConsoleLines();
            screen.InitScreen(ref spinner, " Verifying Transactions...");
            IRippleClient client = new RippleClient(config.websockUrl);
            client.Connect();
            Queue<Airdrop> unVerifiedList = db.GetUnverifiedFailedRecords(config);
            int retries = 0;
            try
            {
                if (unVerifiedList.Count > 0)
                {
                    do
                    {
                        Airdrop a = unVerifiedList.Peek();
                        ITransactionResponseCommon response = await client.Transaction(a.txn_hash);
                        //Throttle
                        Thread.Sleep(500);
                        if (response.Validated != null)
                        {
                            if (response.Validated.Value)
                            {
                                //Validated
                                db.UpdateAirdropRecord(a.address, 1, 1, null);
                                unVerifiedList.Dequeue();
                                retries = 0;
                            }
                            else
                            {
                                //Could not validate
                                retries++;
                                Thread.Sleep(config.txnThrottle * 1000);
                            }
                        }
                        else
                        {
                            //Could not validate
                            retries++;
                            Thread.Sleep(config.txnThrottle * 1000);
                        }

                        if (retries >= 3)
                        {
                            db.UpdateAirdropRecord(a.address, 1, -2, null);
                            unVerifiedList.Dequeue();
                        }
                    } while (unVerifiedList.Count > 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            finally
            {
                client.Disconnect();
                screen.Stop(ref spinner);
                screen.ClearConsoleLines();
                screen.WriteMessages("Transactions Verified.");
            }
        }

        public async Task UpdateTrustlineAccountsAsync()
        {
            screen.ClearConsoleLines();
            screen.InitScreen(ref spinner, " Processing Trustlines...");

            try
            {
                //Delete Trustline accounts first
                db.DeleteTrustLines();

                List<Airdrop> trustlines = new List<Airdrop>();
                string marker = "";
                int count = 0;

                IRippleClient client = new RippleClient(config.websockUrl);
                client.Connect();

                try
                {
                    do
                    {

                        XRPL.TrustLineReturnObj returnObj = await xrpl.ReturnTrustLines(client, config.issuerAddress, marker);
                        count = count + returnObj.trustlines.Count;
                        marker = returnObj.marker;

                        db.AddTrustLineRecords(returnObj.trustlines);
                        screen.ClearConsoleLines(28);
                        screen.WriteMessages("Total trustline accounts added to database: " + count);

                        //Throttle
                        Thread.Sleep(config.accountLinesThrottle * 1000);
                    } while (marker != "" && marker != null);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                finally
                {
                    client.Disconnect();
                }

                db.DeleteDuplicates();
                db.RemoveAirdropAccount(config);
                screen.Stop(ref spinner);
                screen.ClearConsoleLines();
                if (!config.xrplVerifyEnabled)
                {
                    screen.WriteMessages("Successfully Added " + count + " Trustline accounts to the database. Press any key to go back to the menu.");
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                spinner.Stop();
                screen.ClearConsoleLines();
                screen.WriteErrors("Error: " + ex.Message);
            }


        }

        public void UpdateExclusionList()
        {
            if (config.xrpForensicsUrl == "")
            {
                throw new Exception("XRP Forensics URL is not configured");
            }

            screen.ClearConsoleLines();
            screen.InitScreen(ref spinner, " Processing Exclusion List...");

            try
            {
                db.DeleteExclusionData();
                int totalPageCount = 1;
                string responseStr = "";
                List<Models.ExclusionList> list = new List<Models.ExclusionList>();
                using (var webClient = new WebClient())
                {
                    screen.ClearConsoleLines(28);
                    screen.WriteMessages("Getting Page 1");
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(config.xrpForensicsUrl + "?page=0");
                    request.Method = "GET";
                    request.Headers.Add("x-api-key", config.xrpForensicsKey);
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        totalPageCount = Convert.ToInt32(response.Headers["pagination-count"]);
                        list = new List<Models.ExclusionList>();
                        Stream dataStream = response.GetResponseStream();
                        StreamReader reader = new StreamReader(dataStream);
                        responseStr = reader.ReadToEnd();
                        list = JsonConvert.DeserializeObject<List<Models.ExclusionList>>(responseStr);
                        reader.Close();
                        dataStream.Close();
                    }

                    db.AddExclusionListRecords(list);

                    for (int i = 1; i < totalPageCount; i++)
                    {
                        screen.ClearConsoleLines(28);
                        screen.WriteMessages("Getting Page " + (i + 1).ToString() + " of " + totalPageCount);
                        request = (HttpWebRequest)HttpWebRequest.Create(config.xrpForensicsUrl + "?page=" + i);
                        request.Method = "GET";
                        request.Headers.Add("x-api-key", config.xrpForensicsKey);
                        responseStr = "";
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            list = new List<Models.ExclusionList>();
                            Stream dataStream = response.GetResponseStream();
                            StreamReader reader = new StreamReader(dataStream);
                            responseStr = reader.ReadToEnd();
                            list = JsonConvert.DeserializeObject<List<Models.ExclusionList>>(responseStr);
                            reader.Close();
                            dataStream.Close();
                        }

                        db.AddExclusionListRecords(list);

                        //Throttle
                        Thread.Sleep(config.accountLinesThrottle * 1000);
                    }

                }

                screen.Stop(ref spinner);
                screen.ClearConsoleLines();
                screen.WriteMessages("Successfully Added XRP Forensic account data. Press any key to go back to the menu.");
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                spinner.Stop();
                screen.ClearConsoleLines();
                Console.SetCursorPosition(0, 27);
                Console.WriteLine("Error: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}
