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
            PaperSize PSize = new PaperSize("Ticket", 300, 230 + GlobalClass.TCList.Sum(x=>x.Height));
            ps.PaperSize = PSize;
            ps.Margins = new Margins(10, 10, 10, 10);
            ps.Landscape = false;
            PD.DefaultPageSettings = ps;
            PD.Print();
        }


    }
}
