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
            spinner = new Spinner(0, 23);
            engine = new AirdropEngine(config, spinner);
            verify = new Verify(config, spinner);
            tryCreateDirectories();
            while (showMenu)
            {
                showMenu = await MainMenuAsync();
            }
        }

        private static void tryCreateDirectories()
        {
            if(!Directory.Exists(Environment.CurrentDirectory + "\\Reports"))
            {
                System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + "\\Reports");
            }
        }

        private static async Task<bool> MainMenuAsync()
        {
            string VersionNumber = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string asciiTop = File.ReadAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Menu_ascii.txt"));
            string menuSelection = File.ReadAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Menu_selection.txt"));
            Console.Clear();
            Console.WriteLine(asciiTop);
            Console.WriteLine("v" + VersionNumber);
            Console.WriteLine(menuSelection);

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
                    if(config.reportExportFormat == "CSV")
                    {
                        WorkSheetClass.GenerateCSV();
                    } else if (config.reportExportFormat == "XLSX")
                    {
                        WorkSheetClass.GenerateExcelWorkSheet();
                    }
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
                    Console.WriteLine("*** Are you sure you want to do this? *** Y or N");
                    switch (Console.ReadLine())
                    {
                        case "Y":
                            await engine.CreateNewToken();
                            return true;
                        case "N":
                            return true;
                    }
                    return true;
                case "10":
                    Console.WriteLine("*** Are you sure you want to do this? *** ");
                    Console.WriteLine("*** Your issuer account configured as " + config.issuerAddress + " will be blackholed. *** ");
                    Console.WriteLine("*** To confirm, please type \"blackhole\" and press enter. *** ");
                    switch (Console.ReadLine())
                    {
                        case "blackhole":
                            await engine.BlackholeIssuerAccount();
                            return true;
                        default:
                            return true;
                    }
                case "11":
                    Console.WriteLine("*** Are you sure you want to do this? *** Y or N");
                    switch (Console.ReadLine())
                    {
                        case "Y":
                            await engine.SetEmailHash();
                            return true;
                        case "N":
                            return true;
                    }
                    return true;
                case "12":
                    return false;
                default:
                    return true;
            }
        }
    }
}
