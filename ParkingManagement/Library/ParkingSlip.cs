﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarcodeLib;
using System.Drawing;
using System.Drawing.Printing;
using ParkingManagement.Models;
using QRCoder;

namespace ParkingManagement.Library
{
    public class ParkingSlip
    {
        int paperWidth = (GlobalClass.SlipPrinterWith == 58) ? 225 : 300;
        int BarcodeWidth = (GlobalClass.SlipPrinterWith == 58) ? 200 : 250;
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


            G.DrawString(CompanyName, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(0, i, paperWidth, 17), format);
            i += 17;

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(PIN.Barcode, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(3);
            G.DrawImage(qrCodeImage, new Point(180, i));

            G.DrawString(CompanyAddress, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(0, i, paperWidth - 20, 17), format);
            i += 17;

            G.DrawString("Parking Slip", new Font(new FontFamily("Segoe UI Semibold"), 9), Brushes.Black, new RectangleF(0, i, paperWidth - 20, 20), format);
            i += 18;

            G.DrawString(PIN.VType.Description, new Font(new FontFamily("Segoe UI"), 11, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, paperWidth - 20, 18), format);
            i += 20;
            G.DrawString(string.Format("Date : {0} ({1})", PIN.InDate.ToString("MM/dd/yyyy"), PIN.InMiti), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, paperWidth, 18));
            i += 17;
            G.DrawString(string.Format("Time : {0}", PIN.InTime), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, paperWidth, 18));

            if (!string.IsNullOrEmpty(PIN.PlateNo))
            {
                i += 17;
                G.DrawString(string.Format("Plate No : {0}", PIN.PlateNo), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, paperWidth, 18));
            }

            i += 22;

            Barcode barcode = new Barcode()
            {
                Alignment = AlignmentPositions.CENTER,
                Width = BarcodeWidth,
                Height = 50,
                RotateFlipType = RotateFlipType.RotateNoneFlipNone,
                BackColor = Color.White,
                ForeColor = Color.Black,
                LabelFont = new Font(new FontFamily("Segoe UI"), 8)
            };

            Image img = barcode.Encode(TYPE.CODE39, PIN.Barcode);

            G.DrawImage(img, new Point(10, i));
            i += 50;
            format.Alignment = StringAlignment.Center;
            G.DrawString(PIN.Barcode, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(10, i, paperWidth, 24), format);
            i += 15;

            G.DrawString("For your own convenience, Please do not loose this slip.", new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(10, i, paperWidth - 10, 24), format);
            i += 25;

            G.DrawString("Terms & conditions:", new Font(new FontFamily("Segoe UI Semibold"), 5), Brushes.Black, new RectangleF(10, i, paperWidth, 10));
            i += 12;

            int Sno = 1;
            foreach (PSlipTerms tc in GlobalClass.TCList)
            {
                G.DrawString(string.Format("{0}. {1}", Sno, tc.Description), new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, paperWidth, tc.Height));
                Sno++;
                i += tc.Height;
            }
        }
        public void Print()
        {
            PageSettings ps = new PageSettings();
            PaperSize PSize = new PaperSize("Ticket", paperWidth, 230 + GlobalClass.TCList.Sum(x => x.Height));
            ps.PaperSize = PSize;
            if (GlobalClass.SlipPrinterWith == 58)
                ps.Margins = new Margins(5, 5, 5, 5);
            else
                ps.Margins = new Margins(10, 10, 10, 10);
            ps.Landscape = false;
            PD.DefaultPageSettings = ps;
            PD.Print();
        }


    }

    public class VoucherPrint
    {
        Voucher[] Barcodes;
        PrintDocument PD;
        public VoucherPrint(Voucher[] _Barcode)
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
            foreach (Voucher Barcode in Barcodes)
            {

                G.DrawString(Barcode.VoucherNo.ToString(), new Font(new FontFamily("Segoe UI"), 10), Brushes.Black, new RectangleF(70, i + 15, 100, 25), format);
                i += 50;
                Image img = barcode.Encode(TYPE.CODE128, Barcode.Barcode);
                G.DrawImage(img, new Point(450, i));
                i += 50;
                format.Alignment = StringAlignment.Center;
                G.DrawString(string.Format("{0}-{1}", Barcode.Barcode, Barcode.VoucherName), new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(450, i, 290, 24), format);
                i += 190;
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
    public class EntranceTicketPrint
    {

        public string CompanyName { get; set; } = GlobalClass.CompanyName;
        public string CompanyAddress { get; set; } = GlobalClass.CompanyAddress;
        public string VAT { get; set; } = GlobalClass.CompanyPan;
        public bool IsDuplicate { get; set; }
        int paperWidth = (GlobalClass.SlipPrinterWith == 58) ? 225 : 300;
        int BarcodeWidth = (GlobalClass.SlipPrinterWith == 58) ? 200 : 250;

        Voucher Barcodes;
        private readonly TParkingSales vSales;
        private readonly decimal amount;
        private readonly string description;
        PrintDocument PD;
        public EntranceTicketPrint(Voucher _Barcode, TParkingSales vSales, decimal amount, string Description)
        {
            PD = new PrintDocument();
            PD.PrinterSettings.PrinterName = GlobalClass.PrinterName;
            PD.PrintPage += PD_PrintPage;
            Barcodes = _Barcode;
            this.vSales = vSales;
            this.amount = amount;
            description = Description;
        }

        void PD_PrintPage(object sender, PrintPageEventArgs e)
        {
            PrintTicket(e.Graphics);
        }

        private void PrintTicket(Graphics G)
        {

            string EventName;
            string RateName;
            string ShowTime;
            int i = 0;
            //ShowTime = Ticket.show.ShowDate.Add(Ticket.show.StartTime).ToString("dd MMM yyyy dddd hh:mm tt");
            //RateName = (Ticket.IsComplimentary) ? "Complimentary Pass" : (Ticket.Rate.Name + " - " + Ticket.SCat.Name);
            //if (Ticket.Event.Rating != null)
            //    EventName = Ticket.Event.Name + " (" + Ticket.Event.Rating.Rating + ")";
            //else
            //    EventName = Ticket.Event.Name;
            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;

            //if (!Ticket.IsComplimentary)
            //{
            //    G.DrawString("Entrance Pass", new Font(new FontFamily("Segoe UI"), 9, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, 300, 17), format);
            //    i += 17;
            //}


            G.DrawString(CompanyName, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(0, i, 300, 17), format);
            i += 17;

            G.DrawString(CompanyAddress, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(0, i, 300, 17), format);

            i += 17;

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(Barcodes.Barcode, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(3);
            G.DrawImage(qrCodeImage, new Point(180, i));

            i += 17;
            G.DrawString(VAT, new Font(new FontFamily("Segoe UI"), 9), Brushes.Black, new RectangleF(0, i, 300, 17), format);

            if (IsDuplicate)
            {
                i += 17;
                G.DrawString("Duplicate", new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(0, i, 300, 14), format);
            }
            i += 18;
            //G.DrawString("Inv No.  : " + Ticket.sales.BillNo, new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(10, i, 150, 14));
            G.DrawString("Date : " + vSales.TDate.ToString("MM/dd/yyyy"), new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(0, i, 300, 17));
            i += 14;
            //G.DrawString("Time : " + DateTime.Today.Date.Add(Ticket.sales.Time).ToString("hh:mm tt"), new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(150, i, 150, 14));
            G.DrawString("Time : " + vSales.TTime, new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(0, i, 300, 17));
            //if (!Ticket.IsComplimentary)
            //{

            //    G.DrawString("Pay Mode : Cash", new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(10, i, 300, 14));
            //}
            i += 30;
            G.DrawString(description, new Font(new FontFamily("Segoe UI"), 11, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, 300, 20), format);
            i += 20;
            //G.DrawString(ShowTime, new Font(new FontFamily("Segoe UI Semibold"), 9), Brushes.Black, new RectangleF(0, i, 300, 18), format);
            //i += 18;
            //G.DrawString(RateName, new Font(new FontFamily("Segoe UI Semibold"), 9), Brushes.Black, new RectangleF(10, i, 200, 18));
            //G.DrawString("Seat : " + Ticket.seat.SeatNo, new Font(new FontFamily("Segoe UI Semibold"), 9), Brushes.Black, new RectangleF(205, i, 90, 18));

            //i += 22;
            //if (!Ticket.IsComplimentary)
            //{
            //    format.Alignment = StringAlignment.Far;
            //    G.DrawString("Net amount", new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(10, i, 190, 14));
            //    G.DrawString(Ticket.NetAmount.ToString("#0.00"), new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(190, i, 90, 14), format);
            //    i += 14;

            //    foreach (RateTax s in Ticket.Taxes)
            //    {
            //        string taxname = s.Tax.TaxName + " (" + s.Tax.TaxRate + "%)";
            //        G.DrawString(taxname, new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(10, i, 190, 14));
            //        G.DrawString(s.TaxAmnt.ToString("#0.00"), new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(190, i, 90, 14), format);
            //        i += 14;
            //    }

            //    G.DrawLine(new Pen(Brushes.Black), new PointF(10, i), new Point(290, i));
            //    i += 5;
            //    G.DrawString("Gross amount", new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(10, i, 200, 14));
            //    G.DrawString(Ticket.GrossAmount.ToString("#0.00"), new Font(new FontFamily("Segoe UI"), 7), Brushes.Black, new RectangleF(190, i, 90, 14), format);
            //    i += 18;
            //}
            Barcode barcode = new Barcode()
            {
                IncludeLabel = true,
                Alignment = AlignmentPositions.CENTER,
                Width = 250,
                Height = 60,
                RotateFlipType = RotateFlipType.RotateNoneFlipNone,
                BackColor = Color.White,
                ForeColor = Color.Black,
                LabelFont = new Font(new FontFamily("Segoe UI"), 8)
            };

            Image img = barcode.Encode(TYPE.CODE128, Barcodes.Barcode);

            G.DrawImage(img, new Point(10, i));
            i += 65;
            format.Alignment = StringAlignment.Center;
            G.DrawString("Keep the ticket until the end!Have Fun!", new Font(new FontFamily("Segoe UI"), 6), Brushes.Black, new RectangleF(10, i, 290, 24), format);
            i += 25;
            //if (!string.IsNullOrEmpty(Ticket.Event.TicketMessage))
            //{
            //    G.DrawString(Ticket.Event.TicketMessage, new Font(new FontFamily("Segoe UI"), 6), Brushes.Black, new RectangleF(10, i, 290, 24), format);
            //    i += 25;
            //}
            G.DrawString("Terms & conditions:", new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, 300, 10));
            i += 10;
            G.DrawString("1.Tickets once sold cannot be refuned.", new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, 290, 10));
            i += 10;
            G.DrawString("2.Lost, stolen or damaged tickets will not be replaced.", new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, 290, 10));
            i += 10;
            //G.DrawString("3." + CompanyName + " reserves the right to cancel a show, substitute an alternative film or change the timings of the shows if necessary for any reasons.", new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, 290, 20));
            //i += 20;
            //G.DrawString("4.If a film showing is cancelled or it’s timing altered, the cost of purchase of the tickets will either be refunded or replacement tickets for the same movie at an alternative showtime  will be issued. " + CompanyName + " will not be liable to reimburse for any expenses or any losses incurred including travel cost & parking cost.", new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, 290, 40));
            //i += 40;
            //G.DrawString("5.Seat allocations cannot be altered after the purchase of the tickets.", new Font(new FontFamily("Segoe UI"), 5), Brushes.Black, new RectangleF(10, i, 290, 10));
            //i += 25;
            G.DrawString("Enjoy your experience at " + CompanyName + ".", new Font(new FontFamily("Segoe UI"), 6), Brushes.Black, new RectangleF(0, i, 300, 12), format);

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
        public string InWords { get; set; }
        public string DuplicateCaption { get; set; }
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

            G.DrawRectangle(LinePen, new Rectangle(10, 30, PrintWidth - 60, PrintHeight - 90));
            G.DrawLine(LinePen, new Point(10, 150), new Point(PrintWidth - 50, 150));
            G.DrawLine(LinePen, new Point(10, 240), new Point(PrintWidth - 50, 240));
            G.DrawLine(LinePen, new Point(10, 270), new Point(PrintWidth - 50, 270));
            G.DrawLine(LinePen, new Point(10, 800), new Point(PrintWidth - 50, 800));
            G.DrawLine(LinePen, new Point(10, 830), new Point(PrintWidth - 50, 830));
            G.DrawLine(LinePen, new Point(10, 930), new Point(PrintWidth - 50, 930));

            G.DrawLine(LinePen, new Point(60, 240), new Point(60, 830));
            G.DrawLine(LinePen, new Point(450, 240), new Point(450, 830));
            G.DrawLine(LinePen, new Point(550, 240), new Point(550, 930));
            G.DrawLine(LinePen, new Point(650, 240), new Point(650, 800));


            G.DrawString(CompanyName, new Font(new FontFamily(Font), 14, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, PrintWidth, 17), format);
            i += 22;

            G.DrawString(CompanyAddress, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(0, i, PrintWidth, 17), format);
            i += 20;

            G.DrawString("PAN : " + CompanyPan, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 20;

            G.DrawString(InvoiceTitle, new Font(new FontFamily(Font), 10, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 20;

            G.DrawString(DuplicateCaption, new Font(new FontFamily(Font), 10), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 30;

            G.DrawString("Bill No", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(string.Format(": {0}", PSales.BillNo), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, 200, 20));
            G.DrawString("Date", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(PrintWidth - 200, i, 50, 20));
            G.DrawString(": " + PSales.TMiti, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(PrintWidth - 140, i, 100, 20));
            i += 20;

            G.DrawString("Customer Name", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(": " + PSales.BillTo, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, PrintWidth - 125, 20));
            i += 20;

            G.DrawString("Address", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(": " + PSales.BILLTOADD, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, PrintWidth - 125, 20));
            i += 20;

            G.DrawString("PAN No", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(": " + PSales.BILLTOPAN, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, PrintWidth - 125, 20));
            i += 35;


            G.DrawString("S.N. ", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(15, i, 50, 20), format);
            G.DrawString("Particulars", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(60, i, 400, 20), format);
            G.DrawString("Quantity", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(450, i, 100, 20), format);
            G.DrawString("Rate", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(550, i, 100, 20), format);
            G.DrawString("Amount", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);

            i += 25;
            format.Alignment = StringAlignment.Far;
            for (int j = 0; j < PSDetails.Count; j++)
            {
                TParkingSalesDetails psd = PSDetails[j];
                G.DrawString((j + 1).ToString(), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(10, i, 45, 20), format);
                G.DrawString(psd.Description, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(65, i, 400, 20));
                G.DrawString(psd.Quantity.ToString("#0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(450, i, 95, 20), format);
                G.DrawString(psd.Rate.ToString("#0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20), format);
                G.DrawString(psd.Amount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(650, i, 120, 20), format);
                i += 18;
                if (!string.IsNullOrEmpty(psd.Remarks))
                {
                    G.DrawString("  [" + psd.Remarks + "]", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(65, i, 400, 20));
                    i += 18;
                }
            }
            i = 810;
            G.DrawString(PSDetails.Sum(x => x.Quantity).ToString("#0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(450, i, 95, 20), format);
            G.DrawString(PSales.Amount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(650, i, 120, 20), format);

            i += 25;
            G.DrawString("In Words :", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(10, i, 95, 20));
            G.DrawString(InWords, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(10, i + 20, 500, 60));

            G.DrawString("Taxable :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20));
            G.DrawString(PSales.Taxable.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);
            i += 18;

            G.DrawString("Non Taxable :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20));
            G.DrawString(PSales.NonTaxable.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);
            i += 18;

            G.DrawString("VAT :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20));
            G.DrawString(PSales.VAT.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);
            i += 18;

            G.DrawString("Net Amount :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20));
            G.DrawString(PSales.GrossAmount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);
            i += 18;


            G.DrawLine(LinePen, new Point(45, 1075), new Point(205, 1075));
            G.DrawString("Prepared By : " + PSales.Description, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(50, 1080, 200, 20));

            G.DrawLine(LinePen, new Point(PrintWidth - 225, 1075), new Point(PrintWidth - 65, 1075));
            G.DrawString("Received By", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(PrintWidth - 220, 1080, 150, 20));

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

    class CreditNote
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
        public string InWords { get; set; }
        public string DuplicateCaption { get; set; }
        public CreditNote()
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

            G.DrawRectangle(LinePen, new Rectangle(10, 30, PrintWidth - 60, PrintHeight - 90));
            G.DrawLine(LinePen, new Point(10, 150), new Point(PrintWidth - 50, 150));
            G.DrawLine(LinePen, new Point(10, 280), new Point(PrintWidth - 50, 280));
            G.DrawLine(LinePen, new Point(10, 310), new Point(PrintWidth - 50, 310));
            G.DrawLine(LinePen, new Point(10, 800), new Point(PrintWidth - 50, 800));
            G.DrawLine(LinePen, new Point(10, 830), new Point(PrintWidth - 50, 830));
            G.DrawLine(LinePen, new Point(10, 930), new Point(PrintWidth - 50, 930));

            G.DrawLine(LinePen, new Point(60, 280), new Point(60, 830));
            G.DrawLine(LinePen, new Point(450, 280), new Point(450, 830));
            G.DrawLine(LinePen, new Point(550, 280), new Point(550, 930));
            G.DrawLine(LinePen, new Point(650, 280), new Point(650, 800));


            G.DrawString(CompanyName, new Font(new FontFamily(Font), 14, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, PrintWidth, 17), format);
            i += 22;

            G.DrawString(CompanyAddress, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(0, i, PrintWidth, 17), format);
            i += 20;

            G.DrawString("PAN : " + CompanyPan, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 20;

            G.DrawString(InvoiceTitle, new Font(new FontFamily(Font), 10, FontStyle.Bold), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 20;

            G.DrawString(DuplicateCaption, new Font(new FontFamily(Font), 10), Brushes.Black, new RectangleF(0, i, PrintWidth, 20), format);
            i += 30;

            G.DrawString("Bill No", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(string.Format(": {0}", PSales.BillNo), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, 200, 20));
            G.DrawString("Date", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(PrintWidth - 200, i, 50, 20));
            G.DrawString(": " + PSales.TMiti, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(PrintWidth - 140, i, 100, 20));
            i += 20;

            G.DrawString("Customer Name", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(": " + PSales.BillTo, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, PrintWidth - 125, 20));
            i += 20;

            G.DrawString("Address", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(": " + PSales.BILLTOADD, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, PrintWidth - 125, 20));
            i += 20;

            G.DrawString("PAN No", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(": " + PSales.BILLTOPAN, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, PrintWidth - 125, 20));
            i += 20;
            G.DrawString("Ref No", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(": " + PSales.RefBillNo, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, PrintWidth - 125, 20));
            i += 20;
            G.DrawString("C/N Remarks", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(15, i, 100, 20));
            G.DrawString(": " + PSales.Remarks, new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(105, i, PrintWidth - 125, 20));

            i += 35;


            G.DrawString("S.N. ", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(15, i, 50, 20), format);
            G.DrawString("Particulars", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(60, i, 400, 20), format);
            G.DrawString("Quantity", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(450, i, 100, 20), format);
            G.DrawString("Rate", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(550, i, 100, 20), format);
            G.DrawString("Amount", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);

            i += 25;
            format.Alignment = StringAlignment.Far;
            for (int j = 0; j < PSDetails.Count; j++)
            {
                TParkingSalesDetails psd = PSDetails[j];
                G.DrawString((j + 1).ToString(), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(10, i, 45, 20), format);
                G.DrawString(psd.Description, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(65, i, 400, 20));
                G.DrawString(psd.Quantity.ToString("#0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(450, i, 95, 20), format);
                G.DrawString(psd.Rate.ToString("#0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20), format);
                G.DrawString(psd.Amount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(650, i, 120, 20), format);
                i += 18;
            }
            i = 810;
            G.DrawString(PSDetails.Sum(x => x.Quantity).ToString("#0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(450, i, 95, 20), format);
            G.DrawString(PSales.Amount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(650, i, 120, 20), format);

            i += 25;
            G.DrawString("In Words :", new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(10, i, 95, 20));
            G.DrawString(InWords, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(10, i + 20, 500, 60));

            G.DrawString("Taxable :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20));
            G.DrawString(PSales.Taxable.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);
            i += 18;

            G.DrawString("Non Taxable :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20));
            G.DrawString(PSales.NonTaxable.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);
            i += 18;

            G.DrawString("VAT :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20));
            G.DrawString(PSales.VAT.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);
            i += 18;

            G.DrawString("Net Amount :", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(550, i, 95, 20));
            G.DrawString(PSales.GrossAmount.ToString("#,##,##0.00"), new Font(new FontFamily(Font), 9, FontStyle.Bold), Brushes.Black, new RectangleF(650, i, 120, 20), format);
            i += 18;


            G.DrawLine(LinePen, new Point(45, 1075), new Point(205, 1075));
            G.DrawString("Prepared By : " + PSales.Description, new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(50, 1080, 200, 20));

            G.DrawLine(LinePen, new Point(PrintWidth - 225, 1075), new Point(PrintWidth - 65, 1075));
            G.DrawString("Received By", new Font(new FontFamily(Font), 9), Brushes.Black, new RectangleF(PrintWidth - 220, 1080, 150, 20));

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

    public class StringPrint
    {
        string PrintStr;
        PrintDocument PD;
        public StringPrint(string _PrintStr)
        {
            PD = new PrintDocument();
            PD.PrinterSettings.PrinterName = GlobalClass.PrinterName;
            PD.PrintPage += PD_PrintPage;
            PrintStr = _PrintStr;
        }

        void PD_PrintPage(object sender, PrintPageEventArgs e)
        {
            PrintTicket(e.Graphics);
        }

        private void PrintTicket(Graphics G)
        {
            G.DrawString(PrintStr, new Font(new FontFamily("Courier New"), 9), Brushes.Black, new RectangleF(10, 10, 320, 500));
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
