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
                uint sequence = await xrpl.GetLatestAccountSequence(client, config.airdropAddress);

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
                            screen.ClearConsoleLines(24);
                            screen.WriteMessages("Waiting...fees too high. Current Open Ledger Fee: " + f.Drops.OpenLedgerFee, "Fees configured based on fee multiplier: " + Convert.ToInt32(Math.Floor(f.Drops.OpenLedgerFee * config.feeMultiplier)), "Maximum Fee Configured: " + config.maximumFee);
                            Thread.Sleep(config.accountLinesThrottle * 1000);
                            //Get Current Fees
                            f = await client.Fees();
                        }

                        int feeInDrops = Convert.ToInt32(Math.Floor(f.Drops.OpenLedgerFee * config.feeMultiplier));

                        Submit response = await xrpl.SendXRPPaymentAsync(client, a.address, sequence, feeInDrops, config.transferFee);
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
                        else if (response.EngineResult == "tefPAST_SEQ")
                        {
                            //Get new account sequence + try again
                            sequence = await xrpl.GetLatestAccountSequence(client, config.airdropAddress);
                        }
                        else if (response.EngineResult == "telCAN_NOT_QUEUE_FEE")
                        {
                            sequence = await xrpl.GetLatestAccountSequence(client, config.airdropAddress);
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
                    screen.ClearConsoleLines(24);
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
            int totalToVerify = unVerifiedList.Count;
            int retries = 0;
            try
            {
                screen.ClearConsoleLines(24);
                screen.WriteMessages("Verifying Transactions... ", "Total to Verify: " + totalToVerify);
                //Wait a few seconds for ledger finality
                Thread.Sleep(3000);
                if (unVerifiedList.Count > 0)
                {
                    do
                    {
                        Airdrop a = unVerifiedList.Peek();
                        if(await xrpl.isValidTxn(client, a.txn_hash))
                        {
                            db.UpdateAirdropRecord(a.address, 1, 1, null);
                            unVerifiedList.Dequeue();
                            retries = 0;
                            totalToVerify--;
                        } else
                        {
                            //Could not validate
                            retries++;
                            Thread.Sleep(config.txnThrottle * 1000);
                        }
                        Thread.Sleep(500);
                        if (retries >= 3)
                        {
                            db.UpdateAirdropRecord(a.address, 1, -2, null);
                            unVerifiedList.Dequeue();
                            retries = 0;
                            totalToVerify--;
                        }
                        screen.ClearConsoleLines(25);
                        screen.WriteMessages(25, "Total to Verify: " + totalToVerify);
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
                        if (await xrpl.isValidTxn(client, a.txn_hash))
                        {
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
                        Thread.Sleep(500);
                        if (retries >= 3)
                        {
                            db.UpdateAirdropRecord(a.address, 1, -2, null);
                            unVerifiedList.Dequeue();
                            retries = 0;
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
                        screen.ClearConsoleLines(24);
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
                if (!config.xrplVerifyEnabled || config.xrplVerifyPassword == "")
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
                    screen.ClearConsoleLines(24);
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
                        screen.ClearConsoleLines(24);
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
                Console.SetCursorPosition(0, 24);
                Console.WriteLine("Error: " + ex.Message);
                Console.ReadLine();
            }
        }

        public async Task CreateNewToken()
        {
            screen.ClearConsoleLines();
            screen.InitScreen(ref spinner, " Creating New Token (" + Utils.HexToAscii(config.currencyCode) + ")...");
            IRippleClient client = new RippleClient(config.websockUrl);
            client.Connect();
            try
            {
                screen.ClearConsoleLines(24);
                screen.WriteMessages("Creating new token in issuer account...");
                //Get Current Open Ledger Fee
                uint currentLedgerFee = await xrpl.ReturnOpenLedgerFee(client);
                uint sequence = await xrpl.GetLatestAccountSequence(client, config.issuerAddress);
                Submit accountSetResult = await xrpl.AccountSet(client, config.issuerAddress, config.issuerSecret, 8, currentLedgerFee, sequence, config.domain, config.email, config.transferFee);
                if(accountSetResult.EngineResult == "tesSUCCESS" || accountSetResult.EngineResult == "terQUEUED")
                {
                    //Valid Txn, Proceed
                    //Wait for ledger close
                    Thread.Sleep(10000);
                    //Confirm Txn
                    if (!await xrpl.isValidTxn(client, accountSetResult.Transaction.Hash))
                    {
                        throw new Exception("Txn Not valid.");
                    }
                    screen.ClearConsoleLines(24);
                    screen.WriteMessages("Creating trustline from hot wallet to cold wallet...");
                    sequence = await xrpl.GetLatestAccountSequence(client, config.airdropAddress);
                    Submit trustSetResult = await xrpl.TrustSet(client, config.airdropAddress, config.airdropSecret, config.currencyCode, config.issuerAddress, config.supply, 131072, currentLedgerFee, sequence);
                    if (trustSetResult.EngineResult == "tesSUCCESS" || trustSetResult.EngineResult == "terQUEUED")
                    {
                        //Valid Txn, Proceed
                        //Wait for ledger close
                        Thread.Sleep(10000);
                        //Confirm Txn
                        if (!await xrpl.isValidTxn(client, trustSetResult.Transaction.Hash))
                        {
                            throw new Exception("Txn Not valid.");
                        }

                        screen.ClearConsoleLines(24);
                        screen.WriteMessages("Sending tokens from cold wallet to hot wallet...");
                        sequence = await xrpl.GetLatestAccountSequence(client, config.issuerAddress);
                        Submit paymentTxnResult = await xrpl.SendXRPPaymentAsync(client,
                            config.issuerAddress, config.issuerSecret,
                            config.airdropAddress, config.issuerAddress,
                            config.supply, config.currencyCode, currentLedgerFee, sequence);
                        if (paymentTxnResult.EngineResult == "tesSUCCESS" || paymentTxnResult.EngineResult == "terQUEUED")
                        {
                            //Valid Txn, Proceed
                            //Wait for ledger close
                            Thread.Sleep(10000);
                            //Confirm Txn
                            if (!await xrpl.isValidTxn(client, paymentTxnResult.Transaction.Hash))
                            {
                                throw new Exception("Txn Not valid.");
                            }

                            //Success!!
                            screen.ClearConsoleLines(24);
                            screen.Stop(ref spinner);
                            screen.WriteMessages("Token Creation is Successful! Press any key to go back to the main menu.");
                            Console.ReadLine();
                        }
                        else { throw new Exception(paymentTxnResult.EngineResultMessage); }
                    }
                    else { throw new Exception(trustSetResult.EngineResultMessage); }
                } 
                else { throw new Exception(accountSetResult.EngineResultMessage); }


            } catch(Exception ex)
            {
                screen.ClearConsoleLines(24);
                screen.WriteMessages("Error: " + ex.Message);
                screen.Stop(ref spinner);
                Console.ReadLine();
            }
        }

        public async Task BlackholeIssuerAccount()
        {
            screen.ClearConsoleLines();
            screen.InitScreen(ref spinner, "");
            IRippleClient client = new RippleClient(config.websockUrl);
            client.Connect();
            try
            {

                screen.ClearConsoleLines(24);
                screen.WriteMessages("Blackholing Issuer Account...Step 1: Sending remaining XRP to airdrop wallet.");



                //Get Current Open Ledger Fee
                uint currentLedgerFee = await xrpl.ReturnOpenLedgerFee(client);
                uint sequence = await xrpl.GetLatestAccountSequence(client, config.issuerAddress);
                decimal accountBalance = await xrpl.ReturnAccountBalance(client, config.issuerAddress);
                if(accountBalance > 10)
                {
                    //Attempt to send XRP out to the airdrop wallet. Keep 1 XRP for txn to set Regular Key and Disable Master Key
                    decimal amountToSend = accountBalance - 11;
                    if(amountToSend > Convert.ToDecimal(.001))
                    {
                        Submit txnResponse = await xrpl.SendPlainXRP(client, config.issuerAddress, config.issuerSecret, config.airdropAddress, sequence, (int)currentLedgerFee, amountToSend);
                        //Valid Txn, Proceed
                        //Wait for ledger close
                        if (txnResponse.EngineResult == "tesSUCCESS" || txnResponse.EngineResult == "terQUEUED")
                        {
                            Thread.Sleep(10000);
                            //Confirm Txn
                            if (!await xrpl.isValidTxn(client, txnResponse.Transaction.Hash))
                            {
                                throw new Exception("Txn Not valid.");
                            }
                        }
                        else { throw new Exception(txnResponse.EngineResultMessage); }
                    }
                }


                screen.ClearConsoleLines(24);
                screen.WriteMessages("Blackholing Issuer Account...Step 2: Setting regular key to address 1.");
                sequence = await xrpl.GetLatestAccountSequence(client, config.issuerAddress);
                Submit accountSetRegularKeyResponse = await xrpl.SetRegularKey(client, config.issuerAddress, config.issuerSecret, currentLedgerFee, sequence);
                if (accountSetRegularKeyResponse.EngineResult == "tesSUCCESS" || accountSetRegularKeyResponse.EngineResult == "terQUEUED")
                {
                    //Valid Txn, Proceed
                    //Wait for ledger close
                    Thread.Sleep(10000);
                    //Confirm Txn
                    if (!await xrpl.isValidTxn(client, accountSetRegularKeyResponse.Transaction.Hash))
                    {
                        throw new Exception("Txn Not valid.");
                    }
                    screen.ClearConsoleLines(24);
                    screen.WriteMessages("Blackholing Issuer Account...Step 2: Disabling MasterKey");
                    sequence = await xrpl.GetLatestAccountSequence(client, config.issuerAddress);
                    Submit disableMasterKeyResponse = await xrpl.DisableMasterKey(client, config.issuerAddress, config.issuerSecret, currentLedgerFee, sequence, config.transferFee);
                    if (disableMasterKeyResponse.EngineResult == "tesSUCCESS" || disableMasterKeyResponse.EngineResult == "terQUEUED")
                    {
                        //Valid Txn, Proceed
                        //Wait for ledger close
                        Thread.Sleep(10000);
                        //Confirm Txn
                        if (!await xrpl.isValidTxn(client, disableMasterKeyResponse.Transaction.Hash))
                        {
                            throw new Exception("Txn Not valid.");
                        }

                        //Success!!
                        screen.ClearConsoleLines(24);
                        screen.Stop(ref spinner);
                        screen.WriteMessages("Issuer Account has been successfully Blackholed. Press any key to go back to the main menu.");
                        Console.ReadLine();
                    }
                    else { throw new Exception(disableMasterKeyResponse.EngineResultMessage); }
                }
                else { throw new Exception(accountSetRegularKeyResponse.EngineResultMessage); }


            }
            catch (Exception ex)
            {
                screen.ClearConsoleLines(24);
                screen.WriteMessages("Error: " + ex.Message);
                screen.Stop(ref spinner);
                Console.ReadLine();
            }
        }

        public async Task SetEmailHash()
        {
            screen.ClearConsoleLines();
            screen.InitScreen(ref spinner, "");
            IRippleClient client = new RippleClient(config.websockUrl);
            client.Connect();
            try
            {

                screen.ClearConsoleLines(24);
                screen.WriteMessages("Setting Email Hash on Issuer Account...");

                //Get Current Open Ledger Fee
                uint currentLedgerFee = await xrpl.ReturnOpenLedgerFee(client);
                uint sequence = await xrpl.GetLatestAccountSequence(client, config.issuerAddress);

                Submit accountSetResult = await xrpl.AccountSet(client, config.issuerAddress, config.issuerSecret, 8, currentLedgerFee, sequence, config.domain, config.email);
                if (accountSetResult.EngineResult == "tesSUCCESS" || accountSetResult.EngineResult == "terQUEUED")
                {
                    //Valid Txn, Proceed
                    //Wait for ledger close
                    Thread.Sleep(10000);
                    //Confirm Txn
                    if (!await xrpl.isValidTxn(client, accountSetResult.Transaction.Hash))
                    {
                        throw new Exception("Txn Not valid.");
                    }

                    //Success!!
                    screen.ClearConsoleLines(24);
                    screen.Stop(ref spinner);
                    screen.WriteMessages("Issuer Account has been successfully updated!");
                    Console.ReadLine();

                }
                else { throw new Exception(accountSetResult.EngineResultMessage); }



            }
            catch (Exception ex)
            {
                screen.ClearConsoleLines(24);
                screen.WriteMessages("Error: " + ex.Message);
                screen.Stop(ref spinner);
                Console.ReadLine();
            }
        }
    }
}
