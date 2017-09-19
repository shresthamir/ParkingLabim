using Microsoft.Win32;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using Microsoft.Office.Interop.Excel;
using System.Windows.Media;
namespace ParkingManagement.Library.Helpers
{
    class ExportToExcelHelper
    {
        SfDataGrid dataGrid;
        public ExportToExcelHelper(SfDataGrid _dataGrid)
        {
            dataGrid = _dataGrid;
        }
        public void ExportToExcel()
        {
            var workbook = GetExcelWorkBook();

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files(*.xlsx)|*.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                workbook.SaveAs(sfd.FileName);

                //Message box confirmation to view the created Pdf file.
                if (MessageBox.Show("Do you want to view the Excel file?", "Excel file has been created",
                                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    //Launching the Pdf file using the default Application.
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
            }
        }
        public IWorkbook GetExcelWorkBook()
        {
            if (dataGrid == null) 
                return null;
            try
            {
                var options = new ExcelExportingOptions();               
                options.ExportStackedHeaders = true;
                options.ExportingEventHandler = ExportingHandler;
                options.CellsExportingEventHandler = CellExportingHandler;
                options.ExcelVersion = ExcelVersion.Excel2007;
                var document = dataGrid.ExportToExcel(dataGrid.View, options);
                var workbook = document.Excel.Workbooks[0];

                var workSheet = workbook.Worksheets[0];

                workSheet.InsertRow(1, 5, ExcelInsertOptions.FormatDefault);

                for(int i = 0; i<5;i++)
                {
                    workSheet.Rows[i].Merge();
                    var cell = workSheet.Range["A" + (i + 1).ToString()];

                    IStyle style = cell.CellStyle;
                    style.HorizontalAlignment = ExcelHAlign.HAlignCenter;

                    IFont font = style.Font;
                    font.FontName = "Segoe UI";
                    switch(i)
                    {
                        case 0:
                            cell.Value = GlobalClass.CompanyName;
                            font.Size = 16;
                            font.Bold = true;
                            break;
                        case 1:
                            cell.Value = GlobalClass.CompanyAddress;
                            font.Size = 12;
                            break;
                        case 2:
                            cell.Value = GlobalClass.CompanyPan;
                            font.Size = 12;
                            break;
                        case 3:
                            cell.Value = GlobalClass.ReportName;
                            font.Size = 14;
                            font.Bold = true;
                            break;
                        case 4:
                            cell.Value = GlobalClass.ReportParams;
                            font.Size = 12;
                            break;
                    }
                }
                return workbook;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void ExportToCSV()
        {
            var workbook = GetExcelWorkBook();

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "CSV Files(*.csv)|*.csv"
            };

            if (sfd.ShowDialog() == true)
            {
                workbook.SaveAs(sfd.FileName,",");

                //Message box confirmation to view the created Pdf file.
                if (MessageBox.Show("Do you want to view the Excel file?", "Excel file has been created",
                                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    //Launching the Pdf file using the default Application.
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
            }
        }

        public void ExportToXML()
        {
            var workbook = GetExcelWorkBook();

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "XML Files(*.xml)|*.xml"
            };

            if (sfd.ShowDialog() == true)
            {
                workbook.SaveAsXml(sfd.FileName, ExcelXmlSaveType.MSExcel);

                //Message box confirmation to view the created Pdf file.
                if (MessageBox.Show("Do you want to view the Excel file?", "Excel file has been created",
                                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    //Launching the Pdf file using the default Application.
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
            }
        }

        public void ExportToHTML()
        {
            var workbook = GetExcelWorkBook();

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "HTML Files(*.html)|*.html"
            };

            if (sfd.ShowDialog() == true)
            {
                
                workbook.SaveAsHtml(sfd.FileName, Syncfusion.XlsIO.Implementation.HtmlSaveOptions.Default);

                //Message box confirmation to view the created Pdf file.
                if (MessageBox.Show("Do you want to view the Excel file?", "Excel file has been created",
                                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    //Launching the Pdf file using the default Application.
                    System.Diagnostics.Process.Start(sfd.FileName);
                }
            }
        }

        private static void CellExportingHandler(object sender, GridCellExcelExportingEventArgs e)
        {
            //e.Range.CellStyle.Font.Size = 12;
            //e.Range.CellStyle.Font.FontName = "Segoe UI";

            //if (e.ColumnName == "UnitPrice" || e.ColumnName == "UnitsInStock")
            //{
            //    double value = 0;
            //    if (double.TryParse(e.CellValue.ToString(), out value))
            //    {
            //        e.Range.Number = value;
            //        e.Handled = true;
            //    }
            //}
        }

        private static void ExportingHandler(object sender, GridExcelExportingEventArgs e)
        {

            if (e.CellType == ExportCellType.HeaderCell)
            {                
                e.CellStyle.FontInfo.Bold = true;
            }           
            e.CellStyle.FontInfo.Size = 12;
            e.CellStyle.FontInfo.FontName = "Segoe UI";
        }

       
    }
}
