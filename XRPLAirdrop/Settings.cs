using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using XRPLAirdrop.Models;

namespace XRPLAirdrop
{
    public class NewToken
    {
        public string coldWallet { get; set; }
        public string coldWalletSecret { get; set; }
        public string hotWallet { get; set; }
        public string hotWalletSecret { get; set; }
        public string tokenName { get; set; }
        public long supply { get; set; }
    }
    public class Settings
    {
        public string jsonUrl { get; set; }
        public string websockUrl { get; set; }
        public string issuerAddress { get; set; }
        public string airdropAddress { get; set; }
        public string airdropSecret { get; set; }
        public string currencyCode { get; set; }
        public string airdropTokenAmt { get; set; }
        public bool excludeBots { get; set; }
        public string xrpForensicsUrl { get; set; }
        public string xrpForensicsKey { get; set; }
        public int accountLinesThrottle { get; set; }
        public int txnThrottle { get; set; }
        public bool excludeIfUserHasABalance { get; set; }
        public bool includeOnlyIfHolder { get; set; }
        public int numberOfTrustlines { get; set; }
        public decimal includeOnlyIfHolderThreshold { get; set; }
        public decimal feeMultiplier { get; set; }
        public int maximumFee { get; set; }
        public bool xrplVerifyEnabled { get; set; }
        public string xrplVerifyPassword { get; set; }
        public string issuerSecret { get; set; }
        public long supply { get; set; }
        public decimal transferFee { get; set; }
        public string domain { get; set; }
        public string email { get; set; }
        public string reportExportFormat { get; set; }
        public bool standardCurrencyCode { get; set; }
        public AirdropSettings airDropSettings { get; set; }
        public List<string> exlusionWallets { get; set; }
        public Settings()
        {
            airDropSettings = new AirdropSettings();
            string jsonConfig = File.ReadAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "config/settings.json"));
            dynamic d = JObject.Parse(jsonConfig);
            jsonUrl = d.JSON_URL;
            websockUrl = d.WebSocket_URL;
            issuerAddress = d.Issuer_Address;
            airdropAddress = d.Airdrop_Address;
            airdropSecret = d.Airdrop_Address_Secret;
            standardCurrencyCode = d.Standard_Currency_Code;
            string currencyCodeVal = d.Currency_Code.Value;
            if(standardCurrencyCode)
            {
                currencyCode = d.Currency_Code.Value;
            } else
            {
                currencyCode = Utils.AddZeros(Utils.ConvertHex(d.Currency_Code.Value), 40);
            }
            excludeBots = d.Exclude_Bots;
            xrpForensicsKey = d.XRPForensics_API_Key;
            xrpForensicsUrl = d.XRPForensics_URL;
            accountLinesThrottle = d.AccountLinesThrottle;
            txnThrottle = d.TxnThrottle;
            excludeIfUserHasABalance = d.Exclude_if_user_has_a_balance;
            includeOnlyIfHolder = d.Include_only_holders;
            numberOfTrustlines = d.Max_number_of_trustlines;
            includeOnlyIfHolderThreshold = d.Include_only_holders_num_Tokens;
            feeMultiplier = d.FeeMultiplier;
            maximumFee = d.MaximumFee;
            xrplVerifyEnabled = d.XRPLVerify_Enabled;
            xrplVerifyPassword = d.XRLVerify_Password;
            transferFee = d.TransferFee;
            issuerSecret = d.Issuer_Address_Secret;
            supply = d.Supply;
            domain = d.Domain;
            email = d.Email;
            reportExportFormat = d.Report_Format;

            airDropSettings.type = d.Airdrop.Type;
            airDropSettings.airdropTokenAmt = d.Airdrop.Airdrop_Token_Amt;
            airDropSettings.proportionalAmount = d.Airdrop.Proportional_Amount_Of_Tokens;

            try
            {
                exlusionWallets = new List<string>();
                foreach (string s in d.Exclusion_Wallets)
                {
                    exlusionWallets.Add(s);
                }
            }
            catch (Exception) { }
        }
    }

    public class AirdropSettings
    {
        public string type { get; set; }
        public string airdropTokenAmt { get; set; }
        public string proportionalAmount { get; set; }
    }
}
