using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ParkingManagement.Library.Controls
{
    public class ExtendedTextBox : TextBox
    {
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register("IsDropDownOpen", typeof(bool), typeof(ExtendedTextBox));

        public bool IsDropDownOpen
        {
            get { return (bool)this.GetValue(ExtendedTextBox.IsDropDownOpenProperty); }
            set { this.SetValue(ExtendedTextBox.IsDropDownOpenProperty, value); }
        }
    }
}
