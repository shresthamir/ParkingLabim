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
using System.Windows.Shapes;

namespace ParkingManagement.Forms.Master
{
    /// <summary>
    /// Interaction logic for UserSetting.xaml
    /// </summary>
    public partial class UserSetting : UserControl
    {
        public UserSetting()
        {
            InitializeComponent();
            this.DataContext = new ViewModel.UserViewModel();
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectionStart = 0;
            ((TextBox)sender).SelectionLength = ((TextBox)sender).Text.Length;
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if (item != null && !item.IsFocused)
                item.Focus();
        }
    }
}
