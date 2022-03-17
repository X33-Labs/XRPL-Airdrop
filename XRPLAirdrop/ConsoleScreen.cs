using System;
using XRPLAirdrop.Models;

namespace XRPLAirdrop
{
    public class ConsoleScreen
    {
        private static database db;
        private static Settings config;
        public ConsoleScreen(Settings _config)
        {
            config = _config;
            db = new database();
        }

        public void GetCurrentSettings()
        {
            try
            {
                int totalAirdropRecords = db.GetTotalAirdropRecords();
                int totalExclusionRecords = db.GetTotalExclusionListRecords();

                Console.SetCursorPosition(0, 24);
                Console.WriteLine("Database Stats:");
                Console.WriteLine("Total Trustline Records: " + totalAirdropRecords);
                Console.WriteLine("Total Exclusion List Records: " + totalExclusionRecords);
                Console.WriteLine(" ");
                Console.WriteLine("Airdrop Type: " + config.airDropSettings.type);
                if (config.airDropSettings.type == "proportional")
                {
                    DisplayProportionalAirdrop();
                } else
                {
                    Console.WriteLine("Airdrop Amt per address: " + config.airDropSettings.airdropTokenAmt);
                }
                Console.WriteLine("Issuer Account: " + config.issuerAddress);
                Console.WriteLine("Airdrop Account: " + config.airdropAddress);
                if(config.standardCurrencyCode)
                {
                    Console.WriteLine("Currency: " + config.currencyCode);
                } else
                {
                    Console.WriteLine("Currency: " + config.currencyCode + "(" + Utils.HexToAscii(config.currencyCode) + ")");
                }
                Console.WriteLine("Exclude Bots: " + config.excludeBots.ToString());
                Console.WriteLine("Amount of Trustlines: " + config.numberOfTrustlines);
                Console.WriteLine("Include Only Holders: " + config.includeOnlyIfHolder);
                Console.WriteLine("Include Only Holders Minimum Amt: " + config.includeOnlyIfHolderThreshold);
                Console.WriteLine("Exclude if User Has a Balance: " + config.excludeIfUserHasABalance);
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private void DisplayProportionalAirdrop()
        {
            decimal amountPerThreshold = Convert.ToDecimal(config.airDropSettings.proportionalAmount) / (config.supply / config.includeOnlyIfHolderThreshold);
            Console.WriteLine("Amount Per threshold: " + amountPerThreshold.ToString());
        }

        public void ClearConsoleLines(int startingPosition = 23)
        {
            Console.SetCursorPosition(0, startingPosition);
            startingPosition++;
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, startingPosition);
            startingPosition++;
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, startingPosition);
            startingPosition++;
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, startingPosition);
            startingPosition++;
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, startingPosition);
            startingPosition++;
            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, startingPosition);
            startingPosition++;
            Console.WriteLine(new string(' ', Console.WindowWidth));
        }

        public void WriteMessages(params string[] messages)
        {
            int startPos = 24;
            foreach (string s in messages)
            {
                Console.SetCursorPosition(0, startPos);
                Console.WriteLine(s);
                startPos++;
            }
        }

        public void WriteMessages(int startPos, params string[] messages)
        {
            foreach (string s in messages)
            {
                Console.SetCursorPosition(0, startPos);
                Console.WriteLine(s);
                startPos++;
            }
        }

        public void InitScreen(ref Spinner spinner, params string[] messages)
        {
            Console.SetCursorPosition(0, 23);
            spinner = new Spinner(0, 23);
            spinner.Start();
            Console.SetCursorPosition(2, 23);
            foreach (string s in messages)
            {
                Console.WriteLine(s);
            }
        }

        public void InitScreen(params string[] messages)
        {
            Console.SetCursorPosition(2, 23);
            foreach (string s in messages)
            {
                Console.WriteLine(s);
            }
        }

        public void Stop(ref Spinner spinner)
        {
            spinner.Stop();
        }

        public void WriteErrors(params string[] messages)
        {
            int startPos = 24;
            foreach (string s in messages)
            {
                Console.SetCursorPosition(0, startPos);
                Console.WriteLine(s);
                startPos++;
            }
            Console.ReadLine();
        }

    }
}
