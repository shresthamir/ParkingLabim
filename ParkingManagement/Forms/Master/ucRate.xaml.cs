using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParkingManagement.Forms.Master
{
    /// <summary>
    /// Interaction logic for ucRate.xaml
    /// </summary>
    public partial class ucRate : UserControl
    {
        public ucRate()
        {
            InitializeComponent();
            this.DataContext = new ViewModel.RateViewModel();
            
        }

        
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex();
        }
    }
}
