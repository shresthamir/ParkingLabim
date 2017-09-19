using Syncfusion.Pdf.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ParkingManagement
{
    /// <summary>
    /// Interaction logic for HelpViewer.xaml
    /// </summary>
    public partial class HelpViewer : Window
    {
        public HelpViewer()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {


            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "ParkingManagement.Help.pdf";
            
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))            
            {
                byte[] pdfByte = new byte[stream.Length];
                stream.Read(pdfByte, 0, (int)stream.Length);                
                PdfLoadedDocument loadedDocument = new PdfLoadedDocument(pdfByte);
                pdfviewer1.Load(loadedDocument);
            }
        }
    }
}
