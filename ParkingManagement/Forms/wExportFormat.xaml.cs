using ParkingManagement.Library.Helpers;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace ParkingManagement.Forms
{
    /// <summary>
    /// Interaction logic for wExportFormat.xaml
    /// </summary>
    public partial class wExportFormat : Window
    {
        SfDataGrid Report;
        public int ExportFormat { get { return cmbFormat.SelectedIndex; } }

        public wExportFormat(SfDataGrid _report)
        {
            InitializeComponent();
            Report = _report;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).Content.ToString() == "Ok")
            {
                switch (ExportFormat)
                {
                    case 0:
                        new ExportToExcelHelper(Report).ExportToExcel();
                        break;
                    case 1:
                        new ExportToPdfHelper().ExportToPdf(Report);
                        break;
                    case 2:
                        new ExportToExcelHelper(Report).ExportToXML();
                        break;
                    case 3:
                        new ExportToExcelHelper(Report).ExportToCSV();
                        break;
                }
            }
            this.Close();
        }
    }
}
