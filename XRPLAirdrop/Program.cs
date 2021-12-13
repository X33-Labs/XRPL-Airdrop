using System;
using System.IO;
using System.Threading.Tasks;
using XRPLAirdrop.Models;

namespace XRPLAirdrop
{
    class Program
    {
        private static Settings config;
        private static ConsoleScreen screen;
        private static XRPL xrpl;
        private static Spinner spinner;
        private static AirdropEngine engine;
        private static Verify verify;
        static async Task Main(string[] args)
        {
            bool showMenu = true;
            config = new Settings();
            screen = new ConsoleScreen(config);
            xrpl = new XRPL(config);
            spinner = new Spinner(0, 26);
            engine = new AirdropEngine(config, spinner);
            verify = new Verify(config, spinner);
            while (showMenu)
            {
                showMenu = await MainMenuAsync();
            }
        }

        private static async Task<bool> MainMenuAsync()
        {
            string VersionNumber = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string asciiTop = File.ReadAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Menu_ascii.txt"));
            Console.Clear();
            Console.WriteLine(asciiTop);
            Console.WriteLine("v" + VersionNumber);
            Console.WriteLine("");
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1) Update Trustline accounts");
            Console.WriteLine("2) Update XRPforensics data (optional). deprecated on 1/1/22");
            Console.WriteLine("3) View Current Settings");
            Console.WriteLine("4) Start Airdrop");
            Console.WriteLine("5) Export Report");
            Console.WriteLine("6) Requeue Address");
            Console.WriteLine("7) View Current Network Fees");
            Console.WriteLine("8) Re-verify failed Transaction Checks");
            Console.WriteLine("9) Exit");

            Console.Write("\r\nSelect an option: ");

            switch (Console.ReadLine())
            {
                case "1":
                    Console.WriteLine("*** Are you sure you want to do this? This will remove trustline account data from local memory *** Y or N");
                    switch (Console.ReadLine())
                    {
                        case "Y":
                            await engine.UpdateTrustlineAccountsAsync();
                            if (config.xrplVerifyEnabled && config.xrplVerifyPassword != "")
                            {
                                await verify.XRPLVerifyTransactions();
                            }
                            return true;
                        case "N":
                            return true;
                    }
                    return true;
                case "2":
                    Console.WriteLine("*** Are you sure you want to do this? This will remove XRP Forensics account data from local memory *** Y or N");
                    switch (Console.ReadLine())
                    {
                        case "Y":
                            engine.UpdateExclusionList();
                            return true;
                        case "N":
                            return true;
                    }
                    return true;
                case "3":
                    screen.GetCurrentSettings();
                    return true;
                case "4":
                    Console.WriteLine("*** Are you sure you want to do this? *** Y or N");
                    switch (Console.ReadLine())
                    {
                        case "Y":
                            await engine.SendAirDropAsync();
                            return true;
                        case "N":
                            return true;
                    }
                    return true;
                case "5":
                    WorkSheetClass.GenerateExcelWorkSheet();
                    return true;
                case "6":
                    Utils.RequeueAddress();
                    return true;
                case "7":
                    await xrpl.GetCurrentNetworkFeesAsync();
                    return true;
                case "8":
                    await engine.VerifyPendingFailed();
                    return true;
                case "9":
                    return false;
                default:
                    return true;
            }
        }
    }
}
