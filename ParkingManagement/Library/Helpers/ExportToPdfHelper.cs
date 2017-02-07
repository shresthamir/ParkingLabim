using Microsoft.Win32;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.Windows.PdfViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
namespace ParkingManagement.Library.Helpers
{
    class ExportToPdfHelper
    {
        static PdfGridCellStyle cellstyle = new PdfGridCellStyle();
        public ExportToPdfHelper()
        {
            cellstyle = new PdfGridCellStyle();
            cellstyle.StringFormat = new PdfStringFormat() { Alignment = PdfTextAlignment.Right };
            var font = new Font("Segoe UI", 9f, System.Drawing.FontStyle.Regular);
            cellstyle.Font = new PdfTrueTypeFont(font, true);
            cellstyle.Borders.All = new PdfPen(PdfBrushes.DarkGray, 0.2f);
            //CommandManager.RegisterClassCommandBinding(typeof(SfDataGrid), new CommandBinding(ExportToPdf, OnExecuteExportToPdf, OnCanExecuteExportToExcel));
        }

        public void ExportToPdf(SfDataGrid dataGrid)
        {
            if (dataGrid == null) return;
            try
            {
                var options = new PdfExportingOptions();
                options.CellsExportingEventHandler = GridCellPdfExportingEventhandler;
                options.ExportingEventHandler = GridPdfExportingEventhandler;
                options.PageHeaderFooterEventHandler = PdfHeaderFooterEventHandler;                
                options.ExportStackedHeaders = true;
                var document =  dataGrid.ExportToPdf(options);
                MemoryStream stream = new MemoryStream();
                document.Save(stream);
                PdfViewerControl pdfViewer = new PdfViewerControl();
                pdfViewer.Load(stream);
                Window window = new Window();
                window.Content = pdfViewer;
                window.Show();
                
                //var document = new PdfDocument();
                //document.PageSettings.Orientation = PdfPageOrientation.Landscape;
                //document.PageSettings.SetMargins(20);                
                //var page = document.Pages.Add();
                //var pdfGrid = dataGrid.ExportToPdfGrid(dataGrid.View, options);
                
                //var format = new PdfGridLayoutFormat()
                //{
                //    Layout = PdfLayoutType.Paginate,
                //    Break = PdfLayoutBreakType.FitPage                    
                //};
                
                //pdfGrid.Draw(page, new PointF(), format);


                //SaveFileDialog sfd = new SaveFileDialog
                //{
                //    Filter = "PDF Files(*.pdf)|*.pdf"
                //};

                //if (sfd.ShowDialog() == true)
                //{
                //    using (Stream stream = sfd.OpenFile())
                //    {
                //        document.Save(stream);
                //    }

                //    //Message box confirmation to view the created Pdf file.
                //    if (MessageBox.Show("Do you want to view the Pdf file?", "Pdf file has been created",
                //                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                //    {
                //        //Launching the Pdf file using the default Application.
                //        System.Diagnostics.Process.Start(sfd.FileName);
                //    }
                //}
            }
            catch (Exception)
            {

            }
        }



        #region ExportToPdf Event Handlers

        static void GridPdfExportingEventhandler(object sender, GridPdfExportingEventArgs e)
        {
            if (e.CellType == ExportCellType.HeaderCell)
            {
                e.CellStyle.BackgroundBrush = PdfBrushes.LightSteelBlue;
            }
            else if (e.CellType == ExportCellType.GroupCaptionCell)
            {
                e.CellStyle.BackgroundBrush = PdfBrushes.LightGray;
            }
            else if (e.CellType == ExportCellType.GroupSummaryCell)
            {
                e.CellStyle.BackgroundBrush = PdfBrushes.Azure;
            }
            else if (e.CellType == ExportCellType.TableSummaryCell)
            {
                e.CellStyle.BackgroundBrush = PdfBrushes.LightSlateGray;
                e.CellStyle.TextBrush = PdfBrushes.White;
            }
        }

        static void GridCellPdfExportingEventhandler(object sender, GridCellPdfExportingEventArgs e)
        {
            if ((e.ColumnName == "OrderID" || e.ColumnName == "EmployeeID" || e.ColumnName == "OrderDate" || e.ColumnName == "Freight")
                && e.CellType == ExportCellType.RecordCell)
            {
                e.PdfGridCell.Style = cellstyle;
            }
        }

        static void PdfHeaderFooterEventHandler(object sender, PdfHeaderFooterEventArgs e)
        {
            
            var width = e.PdfPage.GetClientSize().Width;
            
            PdfPageTemplateElement header = new PdfPageTemplateElement(width, 80);   
            FormattedText Ft = new FormattedText(GlobalClass.CompanyName, CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Tahoma"), 14, System.Windows.Media.Brushes.Black);
            Ft.SetFontWeight(FontWeights.SemiBold);
            
            header.Graphics.DrawString(GlobalClass.CompanyName,new PdfTrueTypeFont(new Font("Tahoma",14,System.Drawing.FontStyle.Bold)),PdfBrushes.Black, new PointF((width - (float)Ft.Width)/2,0));

            Ft = new FormattedText(GlobalClass.CompanyAddress, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11, System.Windows.Media.Brushes.Black);
            header.Graphics.DrawString(GlobalClass.CompanyAddress, new PdfTrueTypeFont(new Font("Tahoma", 11)), PdfBrushes.Black, new PointF((width - (float)Ft.Width) / 2, 20));

            Ft = new FormattedText(GlobalClass.CompanyPan, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11, System.Windows.Media.Brushes.Black);
            header.Graphics.DrawString(GlobalClass.CompanyPan, new PdfTrueTypeFont(new Font("Tahoma", 11)), PdfBrushes.Black, new PointF((width - (float)Ft.Width) / 2, 35));

            Ft = new FormattedText(GlobalClass.ReportName, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 12, System.Windows.Media.Brushes.Black);
            Ft.SetFontWeight(FontWeights.SemiBold);
            header.Graphics.DrawString(GlobalClass.ReportName, new PdfTrueTypeFont(new Font("Tahoma", 12, System.Drawing.FontStyle.Bold)), PdfBrushes.Black, new PointF((width - (float)Ft.Width) / 2, 50));

            Ft = new FormattedText(GlobalClass.ReportParams, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Tahoma"), 11, System.Windows.Media.Brushes.Black);
            header.Graphics.DrawString(GlobalClass.ReportParams, new PdfTrueTypeFont(new Font("Tahoma", 11, System.Drawing.FontStyle.Bold)), PdfBrushes.Black, new PointF((width - (float)Ft.Width) / 2, 65));



            //header.Graphics.DrawImage(PdfImage.FromFile(@"C:\555.png"), 155, 5, width / 3f, 34);
            e.PdfDocumentTemplate.Top = header;
            
            //PdfPageTemplateElement footer = new PdfPageTemplateElement(width, 30);
            //footer.Graphics.DrawImage(PdfImage.FromFile(@"..\..\Resources\Footer.jpg"), 0, 0);
            //e.PdfDocumentTemplate.Bottom = footer;
        }

        #endregion

        private void DrawToImage(FrameworkElement element)
        {
            
            element.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(element.DesiredSize));

            System.Windows.Media.Imaging.RenderTargetBitmap bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight,
                                                               120.0, 120.0, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(element);

            System.Windows.Media.Imaging.BitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));

            using (Stream s = File.OpenWrite(@"C:\555.png"))
            {
                encoder.Save(s);
            }
        }
      
    }
}
