using Newtonsoft.Json.Linq;
using Ripple.TxSigning;
using RippleDotNet;
using RippleDotNet.Model;
using RippleDotNet.Model.Account;
using RippleDotNet.Model.Server;
using RippleDotNet.Model.Transaction;
using RippleDotNet.Model.Transaction.Interfaces;
using RippleDotNet.Model.Transaction.TransactionTypes;
using RippleDotNet.Requests.Account;
using RippleDotNet.Requests.Transaction;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using XRPLAirdrop.db.models;

namespace XRPLAirdrop
{
    public class XRPL
    {
        private static Settings config;
        private static ConsoleScreen screen;
        public XRPL(Settings _config)
        {
            config = _config;
            screen = new ConsoleScreen(_config);
        }
        public async Task<Submit> SendXRPPaymentAsync(IRippleClient client, string destinationAddress, uint sequence, int feeInDrops)
        {
            try
            {
                IPaymentTransaction paymentTransaction = new PaymentTransaction();
                paymentTransaction.Account = config.airdropAddress;
                paymentTransaction.Destination = destinationAddress;
                paymentTransaction.Amount = new Currency { CurrencyCode = config.currencyCode, Issuer = config.issuerAddress, Value = config.airdropTokenAmt };
                paymentTransaction.Sequence = sequence;
                paymentTransaction.Fee = new Currency { CurrencyCode = "XRP", ValueAsNumber = feeInDrops };

                TxSigner signer = TxSigner.FromSecret(config.airdropSecret);  //secret is not sent to server, offline signing only
                SignedTx signedTx = signer.SignJson(JObject.Parse(paymentTransaction.ToJson()));

                SubmitBlobRequest request = new SubmitBlobRequest();
                request.TransactionBlob = signedTx.TxBlob;

                Submit result = await client.SubmitTransactionBlob(request);

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<int> ReturnAmountToDrop()
        {
            IRippleClient client = new RippleClient(config.websockUrl);
            client.Connect();

            int MaxNumberOfDrops = 1;
            try
            {
                AccountInfo account = await client.AccountInfo(config.airdropAddress);

                decimal totalFreeXRP = account.AccountData.Balance.ValueAsNumber - 1000000;

                AccountLines accountLines = await client.AccountLines(config.airdropAddress);
                foreach (TrustLine line in accountLines.TrustLines)
                {
                    if (line.Currency == config.currencyCode)
                    {
                        MaxNumberOfDrops = Convert.ToInt32(Math.Floor(Convert.ToDecimal(line.Balance) / Convert.ToDecimal(config.airdropTokenAmt)));
                        break;
                    }
                }

                if (MaxNumberOfDrops > 1)
                {
                    //Check to make sure you have enough balance of XRP to send
                    Fee feeObject = await client.Fees();
                    int totalDropsPerTxn = Convert.ToInt32(Math.Ceiling(feeObject.Drops.OpenLedgerFee * config.feeMultiplier));
                    int totalSafeTransactionsPossible = Convert.ToInt32(Math.Floor(totalFreeXRP / totalDropsPerTxn));
                    if (MaxNumberOfDrops > totalSafeTransactionsPossible)
                    {
                        throw new Exception("Not enough XRP to send transactions");
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error in ReturnAmountToDrop(): " + ex.Message);
            }
            finally
            {
                client.Disconnect();
            }

            return MaxNumberOfDrops;
        }

        public async Task GetCurrentNetworkFeesAsync()
        {
            IRippleClient client = new RippleClient(config.websockUrl);
            try
            {
                client.Connect();
                AccountInfo account = await client.AccountInfo(config.airdropAddress);

                decimal totalFreeXRP = account.AccountData.Balance.ValueAsNumber - 1000000;
                do
                {
                    screen.ClearConsoleLines(28);
                    Fee feeObject = await client.Fees();
                    Console.SetCursorPosition(0, 26);
                    Console.WriteLine("Viewing Real time ledger fees...Press any key to exit");
                    Console.WriteLine("Current Minimum Fee:     " + Utils.dropsToXrp(feeObject.Drops.MinimumFee) + " XRP");
                    Console.WriteLine("Current Median Fee:      " + Utils.dropsToXrp(feeObject.Drops.MedianFee) + " XRP");
                    Console.WriteLine("Current Open Ledger Fee: " + Utils.dropsToXrp(feeObject.Drops.OpenLedgerFee) + " XRP");
                    Console.WriteLine("Total Txns you can send: " + Convert.ToInt32(Math.Floor(totalFreeXRP / feeObject.Drops.OpenLedgerFee)) + " Transactions");
                    Thread.Sleep(5000);
                } while (!Console.KeyAvailable);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetCurrentNetworkFeesAsync(): " + ex.Message);
            }
            finally
            {
                client.Disconnect();
            }
        }

        public async Task<TrustLineReturnObj> ReturnTrustLines(IRippleClient client, string issuerAddress, string marker)
        {
            TrustLineReturnObj returnObj = new TrustLineReturnObj();
            List<Airdrop> trustlines = new List<Airdrop>();
            AccountLinesRequest req = new AccountLinesRequest(config.issuerAddress);
            req.Limit = 400;
            if (marker != "")
            {
                req.Marker = marker;
            }
            AccountLines accountLines = await client.AccountLines(req);
            if (accountLines.Marker != null)
            {
                marker = accountLines.Marker.ToString();
            }
            else
            {
                marker = "";
            }
            foreach (TrustLine line in accountLines.TrustLines)
            {
                if (line.Currency == config.currencyCode)
                {
                    trustlines.Add(new Airdrop { address = line.Account, dropped = 0, txn_verified = 0, datetime = 0, balance = Convert.ToDecimal(line.Balance) * -1 });
                }
            }
            returnObj.trustlines = trustlines;
            returnObj.marker = marker;

            return returnObj;
        }

        public async Task<TransactionReturnObj> ReturnTransactions(IRippleClient client, object marker)
        {
            TransactionReturnObj returnObj = new TransactionReturnObj();
            AccountTransactionsRequest req = new AccountTransactionsRequest(config.issuerAddress);
            if (marker != null)
            {
                req.Marker = marker;
            }
            AccountTransactions transactions = await client.AccountTransactions(req);

            returnObj.transactions = transactions;
            returnObj.marker = transactions.Marker;

            return returnObj;
        }

        public struct TrustLineReturnObj
        {
            public List<Airdrop> trustlines { get; set; }
            public string marker { get; set; }
        }

        public struct TransactionReturnObj
        {
            public AccountTransactions transactions { get; set; }
            public object marker { get; set; }
        }


    }
}
