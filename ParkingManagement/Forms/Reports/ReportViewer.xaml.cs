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
using Syncfusion.UI.Xaml.Grid;
using System.ComponentModel;
using ParkingManagement.Library;

namespace ParkingManagement.Forms.Reports
{
    /// <summary>
    /// Interaction logic for ReportViewer.xaml
    /// </summary>
    public partial class ReportViewer : Window
    {
        public ReportViewer()
        {
            InitializeComponent();
        }
    }

    public static class ReportFields
    {
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;
        public static string CName { get { return GlobalClass.CompanyName; } }
        public static string CAddress { get { return GlobalClass.CompanyAddress; } }
        public static bool CNameVisible { get; set; }
        public static string ReportName { get; set; }       
        private static void OnStaticPropertyChanged(string propertyName)
        {
            if (StaticPropertyChanged != null)
                StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
