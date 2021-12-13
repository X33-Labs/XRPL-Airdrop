using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;

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

    }
}
