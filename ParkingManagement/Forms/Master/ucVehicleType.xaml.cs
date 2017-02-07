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
    /// Interaction logic for ucVehicleType.xaml
    /// </summary>
    public partial class ucVehicleType : UserControl
    {
        
        public ucVehicleType()
        {
            InitializeComponent();
            this.DataContext = new ViewModel.VehicleTypeViewModel();
        }
    }
}
