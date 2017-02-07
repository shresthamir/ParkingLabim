using ParkingManagement.Library.Helpers;
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
    /// Interaction logic for ucTouchParkingOut.xaml
    /// </summary>
    public partial class ucCreditNote : UserControl
    {
        public ucCreditNote()
        {
            InitializeComponent();            
            this.DataContext = new CreditNoteViewModel();            
        }

        private void txtBarcode_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Library.Helpers.BaseViewModel.ButtonAction _action = (DataContext as BaseViewModel)._action;
            if (_action == BaseViewModel.ButtonAction.Init)
                e.Handled = true;
        }
    }
}
