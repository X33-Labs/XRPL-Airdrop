using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using XRPLAirdrop.db.models;

namespace XRPLAirdrop
{
    public class WorkSheetClass
    {
        CultureInfo provider = CultureInfo.InvariantCulture;
        public static void GenerateExcelWorkSheet()
        {
            database db = new database();
            //Initialize ExcelEngine.
            using (ExcelPackage excel = new ExcelPackage())
            {
                var format = new OfficeOpenXml.ExcelTextFormat();
                format.Delimiter = ',';
                format.TextQualifier = '"';
                excel.Workbook.Worksheets.Add("Worksheet1");
                excel.Workbook.Worksheets.Add("Worksheet2");
                excel.Workbook.Worksheets.Add("Worksheet3");

                var headerRow = new List<string[]>
                                          {
                                            new string[] { "id", "Address", "Balance", "dropped", "datetime", "txn_verified", "xrplverify.com", "txn_message", "txn_detail", "txn_hash" }
                                          };

                // Determine the header range (e.g. A1:D1)
                string headerRange = "A1:" + Char.ConvertFromUtf32(headerRow[0].Length + 64) + "1";

                // Target a worksheet
                var worksheet = excel.Workbook.Worksheets["Worksheet1"];

                // Popular header row data
                worksheet.Cells[headerRange].LoadFromArrays(headerRow);

                //Format Columns
                worksheet.Column(5).Style.Numberformat.Format = "yyyy/mm/dd H:mm:ss";



                worksheet.Cells[headerRange].Style.Font.Bold = true;
                worksheet.Cells[headerRange].Style.Font.Size = 14;
                worksheet.Cells[headerRange].Style.Font.Color.SetColor(System.Drawing.Color.Black);

                List<Airdrop> airdropList = new List<Airdrop>();
                airdropList = db.GetAllAirdropRecords();
                int worksheetindex = 2;

                foreach (Airdrop a in airdropList)
                {
                    worksheet.Cells[worksheetindex, 1].LoadFromText(a.id + "," + a.address + "," + a.balance + "," + a.dropped + "," + Utils.UnixTimeStampToDateTime(a.datetime).ToString() + "," + a.txn_verified + "," + a.xrpl_verified + "," + a.txn_message + "," + a.txn_detail + ",https://bithomp.com/explorer/" + a.txn_hash);

                    worksheetindex = worksheetindex + 1;
                }

                if (!OperatingSystem.IsLinux())
                {
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }
                bool exists = System.IO.Directory.Exists(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory) + "/Reports");
                if (!exists)
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory) + "/Reports");
                FileInfo excelFile = new FileInfo(@"Reports/Export_Airdrop_Report_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx");

                excel.SaveAs(excelFile);
                Console.WriteLine("Excel Document has been generated. Press ENTER to continue.");
                Console.ReadLine();
            }
        }

    }
}
