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
    /// Interaction logic for EntrySales.xaml
    /// </summary>
    public partial class EntrySales : UserControl
    {
        public EntrySales()
        {
            InitializeComponent();
            this.DataContext = new EntrySalesViewModel();
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectionStart = 0;
            ((TextBox)sender).SelectionLength = ((TextBox)sender).Text.Length;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            {
                if (!ucTouchParkingOut.IsPanNo(e.Text) || (sender as TextBox).Text.Length >= 9)
                    e.Handled = true;
            }
        }
    }
}
