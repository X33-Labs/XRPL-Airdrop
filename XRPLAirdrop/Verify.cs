using RippleDotNet;
using RippleDotNet.Model;
using RippleDotNet.Model.Account;
using RippleDotNet.Model.Transaction.TransactionTypes;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XRPLAirdrop.Models;

namespace XRPLAirdrop
{
    public class Verify
    {
        private static database db;
        private static Spinner spinner;
        private static ConsoleScreen screen;
        private static Settings config;
        private static XRPL xrpl;
        public Verify(Settings _config, Spinner _spinner)
        {
            config = _config;
            screen = new ConsoleScreen(_config);
            spinner = _spinner;
            xrpl = new XRPL(_config);
            db = new database();
        }
        public async Task XRPLVerifyTransactions()
        {
            screen.ClearConsoleLines();
            screen.InitScreen(ref spinner, " XRPLVerify Processing...");
            int count = 0;
            int countVerified = 0;

            try
            {
                object marker = null;

                IRippleClient client = new RippleClient(config.websockUrl);
                client.Connect();

                try
                {
                    do
                    {

                        XRPL.TransactionReturnObj returnObj = await xrpl.ReturnTransactions(client, marker);
                        marker = returnObj.marker;
                        if (returnObj.transactions != null)
                        {
                            count = count + returnObj.transactions.Transactions.Count;
                        }

                        foreach (TransactionSummary tx in returnObj.transactions.Transactions)
                        {
                            if (tx.Validated && tx.Transaction.TransactionType == TransactionType.TrustSet && tx.Transaction.Memos != null)
                            {
                                string address = tx.Transaction.Account;
                                if (tx.Transaction.Memos.Count > 0)
                                {
                                    foreach (Memo txnMemo in tx.Transaction.Memos)
                                    {
                                        try
                                        {
                                            if (VerifyMemo(address, txnMemo.Memo2.MemoDataAsText))
                                            {
                                                countVerified++;
                                                db.UpdateXRPLVerified(address);
                                            }
                                        }
                                        catch (Exception) { }
                                    }
                                }
                            }
                        }

                        screen.ClearConsoleLines(28);
                        screen.WriteMessages("Total transactions processed " + count);

                        //Throttle
                        Thread.Sleep(config.accountLinesThrottle * 1000);
                    } while (marker != null);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
                finally
                {
                    client.Disconnect();
                }

                screen.Stop(ref spinner);
                screen.ClearConsoleLines();
                int totalTrustlines = db.GetTotalAirdropRecords();
                screen.WriteMessages("Successfully Verified " + countVerified + " out of " + totalTrustlines + " total trustlines with XRPLVerify.com. Press any key to go back to the menu.");
                Console.ReadLine();

            }
            catch (Exception ex)
            {
                spinner.Stop();
                screen.ClearConsoleLines();
                screen.WriteErrors("Error: " + ex.Message);
            }


        }

        private bool VerifyMemo(string address, string memoTxt)
        {
            if (memoTxt == Utils.GetHash(address, Encoding.UTF8.GetBytes(config.xrplVerifyPassword)))
                return true;
            else
                return false;

        }
    }
}
