using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Printing;
using System.Runtime.InteropServices;
using System.Drawing.Printing;
using ParkingManagement.Library;
namespace ParkingManagement.Forms.File
{
    /// <summary>
    /// Interaction logic for PrinterSetting.xaml
    /// </summary>
    public partial class PrinterSetting : Window
    {
        public PrinterSetting()
        {
            InitializeComponent();
            cmbPrinter.ItemsSource = new LocalPrintServer().GetPrintQueues().ToList();
            cmbPrinter.DisplayMemberPath = "Name";
            cmbPrinter.SelectedIndex = 0;
            cmbPrinter.Text = GlobalClass.PrinterName;
        }

        private void cmbPrinter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPrinter.SelectedItem != null)
            {
                txtStatus.Text = ((PrintQueue)cmbPrinter.SelectedItem).QueueStatus.ToString();
                txtWhere.Text = ((PrintQueue)cmbPrinter.SelectedItem).Location;
                txtComment.Text = ((PrintQueue)cmbPrinter.SelectedItem).Comment;
            }
        }

        private void btnPrinterProps_Click(object sender, RoutedEventArgs e)
        {
            var printQueue = ((PrintQueue)cmbPrinter.SelectedItem);
            var settings = new PrinterSettings { PrinterName = printQueue.FullName };
            OpenPrinterPropertiesDialog(settings);
        }


        //printer settings
        [DllImport("winspool.Drv", EntryPoint = "DocumentPropertiesW", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern int DocumentProperties(
          IntPtr hwnd,
          IntPtr hPrinter,
          [MarshalAs(UnmanagedType.LPWStr)] string pDeviceName,
          IntPtr pDevModeOutput,
          ref IntPtr pDevModeInput,
          int fMode);

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern bool GlobalFree(IntPtr hMem);

        private void OpenPrinterPropertiesDialog(PrinterSettings printerSettings)
        {
            var handle = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            var hDevMode = printerSettings.GetHdevmode(printerSettings.DefaultPageSettings);
            var pDevMode = GlobalLock(hDevMode);
            var sizeNeeded = DocumentProperties(handle, IntPtr.Zero, printerSettings.PrinterName, pDevMode, ref pDevMode, 0);
            var devModeData = Marshal.AllocHGlobal(sizeNeeded);
            DocumentProperties(handle, IntPtr.Zero, printerSettings.PrinterName, devModeData, ref pDevMode, 14);
            GlobalUnlock(hDevMode);
            printerSettings.SetHdevmode(devModeData);
            printerSettings.DefaultPageSettings.SetHdevmode(devModeData);
            GlobalFree(hDevMode);
            Marshal.FreeHGlobal(devModeData);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
           
            // Set a variable to the My Documents path. 

            if (!Directory.Exists(GlobalClass.AppDataPath))
                Directory.CreateDirectory(GlobalClass.AppDataPath);
            if (System.IO.File.Exists(GlobalClass.AppDataPath + @"\sysPrinter.dat"))
            {
                System.IO.File.Delete(GlobalClass.AppDataPath + @"\sysPrinter.dat");
            }
            // Open a streamwriter to a new text file named "UserInputFile.txt"and write the contents of 
            // the stringbuilder to it. 
            using (StreamWriter outfile = new StreamWriter(GlobalClass.AppDataPath + @"\sysPrinter.dat", true))
            {
                outfile.Write(((PrintQueue)cmbPrinter.SelectedItem).FullName);
            }

            GlobalClass.PrinterName = ((PrintQueue)cmbPrinter.SelectedItem).FullName;
            GlobalClass.printer = new PrintServer().GetPrintQueues().FirstOrDefault(x => x.FullName.Contains(GlobalClass.PrinterName));
            this.Close();
        }

        // Show this dialog.
    }
}
