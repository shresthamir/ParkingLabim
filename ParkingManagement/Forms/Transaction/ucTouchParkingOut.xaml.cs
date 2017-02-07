using ParkingManagement.Library.Helpers;
using ParkingManagement.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for ucTouchParkingOut.xaml
    /// </summary>
    public partial class ucTouchParkingOut : UserControl
    {
        public ucTouchParkingOut()
        {
            InitializeComponent();
            this.DataContext = new POutVMTouch();
        }

        private void txtBarcode_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Library.Helpers.BaseViewModel.ButtonAction _action = (DataContext as BaseViewModel)._action;
            if (_action == BaseViewModel.ButtonAction.Init)
                e.Handled = true;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsPanNo(e.Text) || (sender as TextBox).Text.Length >= 9)
                e.Handled = true;
        }

        public static bool IsPanNo(string text)
        {
            Regex regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
            return !regex.IsMatch(text);
        }
    }
}
