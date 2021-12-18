using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Text;

namespace XRPLAirdrop
{
    public static class Utils
    {
        public static string dropsToXrp(uint drops)
        {
            decimal d = Convert.ToDecimal(drops) / 1000000;
            return String.Format("{0:0.000000}", d);
        }

        public static string GetHash(string address, byte[] salt)
        {
            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: address,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            return hashed;
        }

        public static void RequeueAddress()
        {
            database db = new database();
            Console.WriteLine("Enter Address to requeue");
            db.UpdateDBForAddress(Console.ReadLine());
            Console.WriteLine("Address Updated");
            Console.ReadLine();
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        public static string ConvertHex(String hexString)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(hexString);
                return Convert.ToHexString(bytes);
            }
            catch (Exception) { return ""; }
        }

        public static string HexToAscii(String hexString)
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
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return string.Empty;
        }

        public static string AddZeros(string s, int totalZeros)
        {
            while (s.Length < 40)
            {
                s = s + "0";
            }
            return s;
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
