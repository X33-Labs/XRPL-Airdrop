using Newtonsoft.Json;
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
using RippleDotNet.Responses.Transaction.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using XRPLAirdrop.db.models;
using JsonIgnoreAttribute = Newtonsoft.Json.JsonIgnoreAttribute;

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

        public async Task<Submit> SendPlainXRP(IRippleClient client, string account, string account_secret, string destinationAddress, uint sequence, int feeInDrops, decimal amount)
        {
            try
            {
                IPaymentTransaction paymentTransaction = new PaymentTransaction();
                paymentTransaction.Account = account;
                paymentTransaction.Destination = destinationAddress;
                paymentTransaction.Amount = new Currency { ValueAsXrp = amount };
                paymentTransaction.Sequence = sequence;
                paymentTransaction.Fee = new Currency { CurrencyCode = "XRP", ValueAsNumber = feeInDrops };

                TxSigner signer = TxSigner.FromSecret(account_secret);  //secret is not sent to server, offline signing only
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

        public async Task<Submit> SendXRPPaymentAsync(IRippleClient client, string destinationAddress, uint sequence, int feeInDrops, decimal transferFee = 0)
        {
            try
            {
                IPaymentTransaction paymentTransaction = new PaymentTransaction();
                paymentTransaction.Account = config.airdropAddress;
                paymentTransaction.Destination = destinationAddress;
                paymentTransaction.Amount = new Currency { CurrencyCode = config.currencyCode, Issuer = config.issuerAddress, Value = config.airdropTokenAmt };
                paymentTransaction.Sequence = sequence;
                paymentTransaction.Fee = new Currency { CurrencyCode = "XRP", ValueAsNumber = feeInDrops };
                if(transferFee > 0)
                {
                    paymentTransaction.SendMax = new Currency { CurrencyCode = config.currencyCode, Issuer = config.issuerAddress, Value = (config.airdropTokenAmt + (Convert.ToDecimal(config.airdropTokenAmt) * (transferFee / 100))).ToString() };
                }

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

        public async Task<Submit> SendXRPPaymentAsync(IRippleClient client, string account, string account_secret, string destination, string issuer, long amount, string currency, uint feeInDrops, uint sequence)
        {
            try
            {

                IPaymentTransaction paymentTransaction = new PaymentTransaction();
                paymentTransaction.Account = account;
                paymentTransaction.Destination = destination;
                paymentTransaction.Sequence = sequence;
                paymentTransaction.Amount = new Currency { CurrencyCode = currency, Issuer = issuer, Value = amount.ToString() };
                paymentTransaction.Fee = new Currency { CurrencyCode = "XRP", ValueAsNumber = feeInDrops };

                TxSigner signer = TxSigner.FromSecret(account_secret);  //secret is not sent to server, offline signing only
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

        public async Task<Submit> AccountSet(IRippleClient client, string account, string account_secret, uint setFlag, uint feeInDrops, uint sequence, string domain = "", string email = "", decimal transferRate = 0)
        {
            try
            {
                AccountSetTxn AccountSetTxn = new AccountSetTxn();
                AccountSetTxn.TransactionType = "AccountSet";
                AccountSetTxn.Account = account;
                AccountSetTxn.SetFlag = setFlag;
                AccountSetTxn.Flags = 1048576;
                AccountSetTxn.Fee = feeInDrops;
                AccountSetTxn.Sequence = sequence;
                if(transferRate > 0)
                {
                    AccountSetTxn.TransferRate = 1000000000 + (uint)Math.Round((1000000000 * (transferRate / 100)));
                }
                if (domain != "")
                {
                    AccountSetTxn.Domain = Utils.ConvertHex(domain);
                }
                if(email != "")
                {
                    AccountSetTxn.EmailHash = Utils.CreateMD5(email);
                }

                JObject j = JObject.Parse(JsonConvert.SerializeObject(AccountSetTxn));
                if (j["Domain"].ToString() == "")
                {
                    j.Remove("Domain");
                }
                if (j["EmailHash"].ToString() == "")
                {
                    j.Remove("EmailHash");
                }
                j.ToString();

                string json = j.ToString();
                TxSigner signer = TxSigner.FromSecret(account_secret);  //secret is not sent to server, offline signing only
                SignedTx signedTx = signer.SignJson(JObject.Parse(json));

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

        public async Task<Submit> TrustSet(IRippleClient client, string account, string account_secret, string currencyCode, string issuer, long supply, uint flags, uint feeInDrops, uint sequence)
        {
            try
            {
                TrustSetTxn trustSetTxn = new TrustSetTxn();
                trustSetTxn.TransactionType = "TrustSet";
                trustSetTxn.Account = account;

                trustSetTxn.LimitAmount = new LimitAmount();
                trustSetTxn.LimitAmount.currency = currencyCode;
                trustSetTxn.LimitAmount.issuer = issuer;
                trustSetTxn.LimitAmount.value = supply.ToString();
                trustSetTxn.Flags = flags;
                trustSetTxn.Fee = feeInDrops;
                trustSetTxn.Sequence = sequence;

                string json = JsonConvert.SerializeObject(trustSetTxn);
                TxSigner signer = TxSigner.FromSecret(account_secret);  //secret is not sent to server, offline signing only
                SignedTx signedTx = signer.SignJson(JObject.Parse(json));

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

        public async Task<Submit> SetRegularKey(IRippleClient client, string account, string account_secret, uint feeInDrops, uint sequence)
        {
            try
            {
                SetRegularKeyTxn setRegularKeyTxn = new SetRegularKeyTxn();
                setRegularKeyTxn.TransactionType = "SetRegularKey";
                setRegularKeyTxn.Account = account;
                setRegularKeyTxn.RegularKey = "rrrrrrrrrrrrrrrrrrrrBZbvji";
                setRegularKeyTxn.Fee = feeInDrops;
                setRegularKeyTxn.Flags = 0;
                setRegularKeyTxn.Sequence = sequence;

                string json = JsonConvert.SerializeObject(setRegularKeyTxn);
                TxSigner signer = TxSigner.FromSecret(account_secret);  //secret is not sent to server, offline signing only
                SignedTx signedTx = signer.SignJson(JObject.Parse(json));

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

        public async Task<Submit> DisableMasterKey(IRippleClient client, string account, string account_secret,uint feeInDrops, uint sequence, decimal transferRate = 0)
        {
            try
            {
                AccountSetTxn accountSetTxn = new AccountSetTxn();
                accountSetTxn.TransactionType = "AccountSet";
                accountSetTxn.Account = account;
                accountSetTxn.SetFlag = 4;
                accountSetTxn.Fee = feeInDrops;
                accountSetTxn.Sequence = sequence;
                if (transferRate > 0)
                {
                    accountSetTxn.TransferRate = 1000000000 + (uint)Math.Round((1000000000 * (transferRate / 100)));
                }

                JObject j = JObject.Parse(JsonConvert.SerializeObject(accountSetTxn));
                if (j["Domain"].ToString() == "")
                {
                    j.Remove("Domain");
                }
                if (j["EmailHash"].ToString() == "")
                {
                    j.Remove("EmailHash");
                }
                j.ToString();

                string json = j.ToString();
                TxSigner signer = TxSigner.FromSecret(account_secret);  //secret is not sent to server, offline signing only
                SignedTx signedTx = signer.SignJson(JObject.Parse(json));

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

                AccountLinesRequest req = new AccountLinesRequest(config.airdropAddress);
                AccountLines accountLines = await client.AccountLines(req);
                if(accountLines.TrustLines.Count == 0)
                {
                    throw new Exception("Error when trying to retrieve Trustlines on airdrop account.");
                }
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

        public async Task<uint> ReturnOpenLedgerFee(IRippleClient client)
        {
            try
            {

                Fee feeObject = await client.Fees();
                return feeObject.Drops.OpenLedgerFee;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<uint> GetLatestAccountSequence(IRippleClient client, string account)
        {
            try
            {

                AccountInfo accountInfo = await client.AccountInfo(account);
                return accountInfo.AccountData.Sequence;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<decimal> ReturnAccountBalance(IRippleClient client, string account)
        {
            try
            {

                AccountInfo accountInfo = await client.AccountInfo(account);
                return accountInfo.AccountData.Balance.ValueAsXrp.HasValue ? accountInfo.AccountData.Balance.ValueAsXrp.Value : 0;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<bool> isValidTxn(IRippleClient client, string txnHash)
        {
            try
            {
                ITransactionResponseCommon response = await client.Transaction(txnHash);
                if (response.Validated != null)
                {
                    if (response.Validated.Value)
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
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
                    //Wrap in try for tiny balance bug
                    try
                    {
                        trustlines.Add(new Airdrop { address = line.Account, dropped = 0, txn_verified = 0, datetime = 0, balance = Convert.ToDecimal(line.Balance) * -1 });
                    } catch(Exception) {
                        trustlines.Add(new Airdrop { address = line.Account, dropped = 0, txn_verified = 0, datetime = 0, balance = 0 });
                    }
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

        public class AccountSetTxn
        {
            public string TransactionType { get; set; }
            public string Account { get; set; }
            public uint SetFlag { get; set; }
            public uint Flags { get; set; }
            public uint Fee { get; set; }
            public uint Sequence { get; set; }
            public string Domain { get; set; }
            public string EmailHash { get; set; }
            public uint TransferRate { get; set; }

            public AccountSetTxn()
            {
                TransferRate = 0;
            }
        }

        public class DisableMasterKeyObj
        {
            public string TransactionType { get; set; }
            public string Account { get; set; }
            public uint SetFlag { get; set; }
            public uint Flags { get; set; }
            public uint Fee { get; set; }
            public uint Sequence { get; set; }
        }

        public class TrustSetTxn
        {
            public string TransactionType { get; set; }
            public string Account { get; set; }
            public uint Flags { get; set; }
            public uint Fee { get; set; }
            public uint Sequence { get; set; }
            public LimitAmount LimitAmount { get; set; }
           // public Memo[] Memo { get; set; }
        }

        public class SetRegularKeyTxn
        {
            public string TransactionType { get; set; }
            public string Account { get; set; }
            public uint Flags { get; set; }
            public uint Fee { get; set; }
            public string RegularKey { get; set; }
            public uint Sequence { get; set; }
        }

        public class LimitAmount
        {
            public string currency { get; set; }
            public string issuer { get; set; }
            public string value { get; set; }
        }

        public class Memo
        {
            public Memo(string _memo)
            {
                MemoData = _memo;
            }
            public string MemoData { get; set; }
        }
    }
}
