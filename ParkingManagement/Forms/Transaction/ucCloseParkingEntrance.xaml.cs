using ParkingManagement.ViewModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ParkingManagement.Forms.Transaction
{
    /// <summary>
    /// Interaction logic for ucCloseParkingEntrance.xaml
    /// </summary>
    public partial class ucCloseParkingEntrance : UserControl
    {
        public ucCloseParkingEntrance()
        {
            this.DataContext = new ParkingEntranceCloseViewModel();
            InitializeComponent();
        }
    }
}
