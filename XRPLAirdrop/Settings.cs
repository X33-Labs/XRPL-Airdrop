using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace XRPLAirdrop
{
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
        public Settings()
        {
            string jsonConfig = File.ReadAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "config/settings.json"));
            dynamic d = JObject.Parse(jsonConfig);
            jsonUrl = d.JSON_URL;
            websockUrl = d.WebSocket_URL;
            issuerAddress = d.Issuer_Address;
            airdropAddress = d.Airdrop_Address;
            airdropSecret = d.Airdrop_Address_Secret;
            currencyCode = d.Currency_Code;
            airdropTokenAmt = d.Airdrop_Token_Amt;
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
        }

        public static string ConvertHex(String hexString)
        {
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    uint decval = System.Convert.ToUInt32(hs, 16);
                    char character = System.Convert.ToChar(decval);
                    ascii += character;

                }

                return ascii;
            }
            catch (Exception) { return ""; }

        }
    }
}
