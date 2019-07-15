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
    /// Interaction logic for Deduction.xaml
    /// </summary>
    public partial class Deduction : UserControl
    {
        public POutVMTouch ViewModel { get; set; }
        public Deduction()
        {
            InitializeComponent();
            this.DataContext =ViewModel = new POutVMTouch();
            Loaded += UserControl_Loaded;
        }

        private void TxtBarCode_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            e.Handled = true;
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtBarCode.Focus();
            ViewModel.TrnMode = "Manual Deduction";
        }
    }
}
