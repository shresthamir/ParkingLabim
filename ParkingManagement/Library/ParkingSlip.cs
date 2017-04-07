using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarcodeLib;
using System.Drawing;
using System.Drawing.Printing;
using ParkingManagement.Models;
namespace ParkingManagement.Library
{
    public class ParkingSlip
    {
        PrintDocument PD;
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public ParkingIn PIN { get; set; }
        public ParkingSlip()
        {
            PD = new PrintDocument();
            PD.PrinterSettings.PrinterName = GlobalClass.PrinterName;
            PD.PrintPage += PD_PrintPage;
        }

        void PD_PrintPage(object sender, PrintPageEventArgs e)
        {
            PrintTicket(e.Graphics);
        }

        private void PrintTicket(Graphics G)
        {
            int i = 0;

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;


            G.DrawString(CompanyName, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(0, i, 300, 17), format);
            i += 17;

            G.DrawString(CompanyAddress, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(0, i, 300, 17), format);
            i += 17;

            G.DrawString("Parking Slip", new Font(new FontFamily("Segoe UI Semibold"), 9), Brushes.Black, new RectangleF(0, i, 300, 20), format);
            i += 18;

            G.DrawString(PIN.VType.Description, new Font(new FontFamily("Segoe UI"), 11, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, 300, 18), format);
            i += 20;
            G.DrawString(string.Format("Date : {0} ({1})", PIN.InDate.ToString("MM/dd/yyyy"), PIN.InMiti), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, 300, 18));
            i += 17;
            G.DrawString(string.Format("Time : {0}", PIN.InTime), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, 300, 18));

            if (!string.IsNullOrEmpty(PIN.PlateNo))
            {
                i += 17;
                G.DrawString(string.Format("Plate No : {0}", PIN.PlateNo), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, 300, 18));
            }

            i += 22;

            Barcode barcode = new Barcode()
            {
                Alignment = AlignmentPositions.CENTER,
                Width = 250,
                Height = 50,
                RotateFlipType = RotateFlipType.RotateNoneFlipNone,
                BackColor = Color.White,
                ForeColor = Color.Black,
                LabelFont = new Font(new FontFamily("Segoe UI"), 8)
            };

            Image img = barcode.Encode(TYPE.CODE128, PIN.Barcode);

            G.DrawImage(img, new Point(10, i));
            i += 50;
            format.Alignment = StringAlignment.Center;
            G.DrawString(PIN.Barcode, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, 290, 24), format);
            i += 15;

            G.DrawString("For your own convenience, Please do not loose this slip.", new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(10, i, 290, 24), format);
            i += 25;

            G.DrawString("Terms & conditions:", new Font(new FontFamily("Segoe UI Semibold"), 5), Brushes.Black, new RectangleF(10, i, 300, 10));
            i += 12;

            int Sno = 1;
            foreach (PSlipTerms tc in GlobalClass.TCList)
            {
                G.DrawString(string.Format("{0}. {1}", Sno, tc.Description), new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, 290, tc.Height));
                Sno++;
                i += tc.Height;
            }
        }
        public void Print()
        {
            PageSettings ps = new PageSettings();
            PaperSize PSize = new PaperSize("Ticket", 300, 230 + GlobalClass.TCList.Sum(x => x.Height));
            ps.PaperSize = PSize;
            ps.Margins = new Margins(10, 10, 10, 10);
            ps.Landscape = false;
            PD.DefaultPageSettings = ps;
            PD.Print();
        }


    }

    public class VoucherPrint
    {
        string[] Barcodes;
        PrintDocument PD;
        public VoucherPrint(string[] _Barcode)
        {
            PD = new PrintDocument();
            PD.PrinterSettings.PrinterName = GlobalClass.PrinterName;
            PD.PrintPage += PD_PrintPage;
            Barcodes = _Barcode;
        }

        void PD_PrintPage(object sender, PrintPageEventArgs e)
        {
            PrintTicket(e.Graphics);
        }

        private void PrintTicket(Graphics G)
        {

            int i = 0;
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            Barcode barcode = new Barcode()
            {
                Alignment = AlignmentPositions.CENTER,
                Width = 250,
                Height = 50,
                RotateFlipType = RotateFlipType.RotateNoneFlipNone,
                BackColor = Color.White,
                ForeColor = Color.Black,
                LabelFont = new Font(new FontFamily("Segoe UI"), 8)
            };
            foreach (string Barcode in Barcodes)
            {
                i += 50;
                Image img = barcode.Encode(TYPE.CODE128, Barcode);
                G.DrawImage(img, new Point(400, i));
                i += 50;
                format.Alignment = StringAlignment.Center;
                G.DrawString(Barcode, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(400, i, 290, 24), format);
                i += 150;
            }
        }

        public void Print()
        {
            PageSettings ps = new PageSettings();
            PaperSize PSize = new PaperSize() { RawKind = (int)PaperKind.A4 };
            ps.PaperSize = PSize;
            ps.Margins = new Margins(10, 10, 10, 10);
            ps.Landscape = false;
            PD.DefaultPageSettings = ps;
            PD.Print();
        }
    }

    class BillPrint
    {
        PrintDocument PD;
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public int PrintWidth, PrintHeight;
        string Font = "Segoe UI";
        public TParkingSales PSales { get; set; }
        public IList<TParkingSalesDetails> PSDetails { get; set; }
        public string CompanyPan { get; set; }
        public string InvoiceTitle { get; set; }
        public BillPrint()
        {
            PD = new PrintDocument();
            PD.PrinterSettings.PrinterName = GlobalClass.PrinterName;
            foreach (PaperSize PSize in PD.PrinterSettings.PaperSizes)
            {
                if (PSize.RawKind == (int)PaperKind.A4)
                {
                    PrintWidth = PSize.Width;
                    PrintHeight = PSize.Height;
                    break;
                }
            }
            PD.PrintPage += PD_PrintPage;
        }

        void PD_PrintPage(object sender, PrintPageEventArgs e)
        {
            PrintTicket(e.Graphics);
        }

        private void PrintTicket(Graphics G)
        {
            int i = 40;
            Pen LinePen = new Pen(Brushes.Black);
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;

            G.DrawRectangle(LinePen, new Rectangle(30, 30, PrintWidth - 60, PrintHeight - 60));
            G.DrawLine(LinePen, new Point(30, 150), new Point(PrintWidth - 30, 150));
            G.DrawLine(LinePen, new Point(30, 240), new Point(PrintWidth - 30, 240));
            G.DrawLine(LinePen, new Point(30, 270), new Point(PrintWidth - 30, 270));
            G.DrawLine(LinePen, new Point(30, 800), new Point(PrintWidth - 30, 800));
            G.DrawLine(LinePen, new Point(30, 830), new Point(PrintWidth - 30, 830));
            G.DrawLine(LinePen, new Point(30, 930), new Point(PrintWidth - 30, 930));

            G.DrawLine(LinePen, new Point(80, 240), new Point(80, 830));
            G.DrawLine(LinePen, new Point(470, 240), new Point(470, 830));
            G.DrawLine(LinePen, new Point(570, 240), new Point(570, 930));
            G.DrawLine(LinePen, new Point(670, 240), new Point(670, 800));


            G.DrawString(CompanyName, new Font(new FontFamily(Font), 14, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, PrintWidth, 17), format);
            i += 22;

            G.DrawString(CompanyAddress, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(0, i, PrintWidth, 17), format);
            i += 20;

            G.DrawString("PAN : " + CompanyPan, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 20;

            G.DrawString(InvoiceTitle, new Font(new FontFamily(Font), 10, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 20;

            G.DrawString("Copy of Original", new Font(new FontFamily(Font), 10), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 30;

            G.DrawString("Bill No", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(35, i, 100, 20));
            G.DrawString(string.Format(": {0}", PSales.BillNo), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(125, i, 200, 20));
            G.DrawString("Date", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(PrintWidth - 180, i, 50, 20));
            G.DrawString(": " + PSales.TMiti, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(PrintWidth - 120, i, 100, 20));
            i += 20;

            G.DrawString("Customer Name", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(35, i, 100, 20));
            G.DrawString(": " + PSales.BillTo, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(125, i, PrintWidth - 125, 20));
            i += 20;

            G.DrawString("Address", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(35, i, 100, 20));
            G.DrawString(": " + PSales.BILLTOADD, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(125, i, PrintWidth - 125, 20));
            i += 20;

            G.DrawString("PAN No", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(35, i, 100, 20));
            G.DrawString(": " + PSales.BILLTOPAN, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(125, i, PrintWidth - 125, 20));
            i += 35;


            G.DrawString("S.N. ", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(35, i, 50, 20), format);
            G.DrawString("Particulars", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(80, i, 400, 20), format);
            G.DrawString("Quantity", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(470, i, 100, 20), format);
            G.DrawString("Rate", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(570, i, 100, 20), format);
            G.DrawString("Amount", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(670, i, 120, 20), format);

            i += 25;
            format.Alignment = StringAlignment.Far;
            for (int j = 0; j < PSDetails.Count; j++)
            {
                TParkingSalesDetails psd = PSDetails[j];
                G.DrawString((j + 1).ToString(), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(30, i, 45, 20), format);
                G.DrawString(psd.Description, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(85, i, 400, 20));
                G.DrawString(psd.Quantity.ToString("#0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(470, i, 95, 20), format);
                G.DrawString(psd.Rate.ToString("#0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(570, i, 95, 20), format);
                G.DrawString(psd.Amount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(670, i, 120, 20), format);
                i += 18;
            }
            i = 810;
            G.DrawString(PSDetails.Sum(x => x.Quantity).ToString("#0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(470, i, 95, 20), format);
            G.DrawString(PSales.Amount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(670, i, 120, 20), format);

            i += 25;
            G.DrawString("Taxable :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(570, i, 95, 20));
            G.DrawString(PSales.Taxable.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(670, i, 120, 20), format);
            i += 18;

            G.DrawString("Non Taxable :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(570, i, 95, 20));
            G.DrawString(PSales.NonTaxable.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(670, i, 120, 20), format);
            i += 18;

            G.DrawString("VAT :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(570, i, 95, 20));
            G.DrawString(PSales.VAT.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(670, i, 120, 20), format);
            i += 18;

            G.DrawString("Net Amount :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(570, i, 95, 20));
            G.DrawString(PSales.GrossAmount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(670, i, 120, 20), format);
            i += 18;

            //G.DrawString(PIN.VType.Description, new Font(new FontFamily("Segoe UI"), 11, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, 300, 18), format);
            //i += 20;
            //G.DrawString(string.Format("Date : {0} ({1})", PIN.InDate.ToString("MM/dd/yyyy"), PIN.InMiti), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, 300, 18));
            //i += 17;
            //G.DrawString(string.Format("Time : {0}", PIN.InTime), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, 300, 18));

            //if (!string.IsNullOrEmpty(PIN.PlateNo))
            //{
            //    i += 17;
            //    G.DrawString(string.Format("Plate No : {0}", PIN.PlateNo), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, 300, 18));
            //}

            //i += 22;

            //Barcode barcode = new Barcode()
            //{
            //    Alignment = AlignmentPositions.CENTER,
            //    Width = 250,
            //    Height = 50,
            //    RotateFlipType = RotateFlipType.RotateNoneFlipNone,
            //    BackColor = Color.White,
            //    ForeColor = Color.Black,
            //    LabelFont = new Font(new FontFamily("Segoe UI"), 8)
            //};

            //Image img = barcode.Encode(TYPE.CODE128, PIN.Barcode);

            //G.DrawImage(img, new Point(10, i));
            //i += 50;
            //format.Alignment = StringAlignment.Center;
            //G.DrawString(PIN.Barcode, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, 290, 24), format);
            //i += 15;

            //G.DrawString("For your own convenience, Please do not loose this slip.", new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(10, i, 290, 24), format);
            //i += 25;

            //G.DrawString("Terms & conditions:", new Font(new FontFamily("Segoe UI Semibold"), 5), Brushes.Black, new RectangleF(10, i, 300, 10));
            //i += 12;

            //int Sno = 1;
            //foreach (PSlipTerms tc in GlobalClass.TCList)
            //{
            //    G.DrawString(string.Format("{0}. {1}", Sno, tc.Description), new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, 290, tc.Height));
            //    Sno++;
            //    i += tc.Height;
            //}
        }
        public void Print()
        {
            PageSettings ps = new PageSettings();
            PaperSize PSize = new PaperSize() { RawKind = (int)PaperKind.A4 };
            ps.PaperSize = PSize;
            ps.Margins = new Margins(10, 10, 10, 10);
            ps.Landscape = false;
            PD.DefaultPageSettings = ps;

            PD.Print();
        }
    }
}
