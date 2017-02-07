using ParkingManagement.Models;
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

namespace ParkingManagement.Forms.Transaction
{
    /// <summary>
    /// Interaction logic for Denomination.xaml
    /// </summary>
    public partial class wDenomination : Window
    {
        public wDenomination()
        {
            InitializeComponent();
        }

        private void Textbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            decimal result;
            if (!(decimal.TryParse(e.Text, out result)))
            {
                e.Handled = true;
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.SelectionStart = 0;
            tb.SelectionLength = tb.Text.Length;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as Button).Content.ToString())
            {
                case "Ok":
                    this.Close();
                    break;
                case "Close":
                    this.DataContext = new Denomination();
                    this.Close();
                    break;
            }
        }
    }
}
