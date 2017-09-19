using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WPFCustomToolKit;
namespace ParkingManagement.Library
{
    class TogleButton : ImageButton
    {
        private string _Text;
        private string _AlternateText;
        
        public string Text { get { return _Text; } set { _Text = value; ButtonText = value; } }
        public string AlternateText { get { return _AlternateText; } set { _AlternateText = value; } }

        public static void SetIsChecked(UIElement element, bool value)
        {
            element.SetValue(IsCheckedProperty, value);
        }
        public static bool GetIsChecked(UIElement element)
        {
            return (bool)element.GetValue(IsCheckedProperty);
        }

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.RegisterAttached("IsChecked", typeof(bool), typeof(TogleButton), new FrameworkPropertyMetadata(false,FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public bool IsChecked
        {
            get
            {
                return (bool)GetValue(IsCheckedProperty);
            }
            set
            {
                SetValue(IsCheckedProperty, value);
                if (value)
                    ButtonText = AlternateText;
                else
                    ButtonText = Text;
            }
        }

        TextBlock _Content;
        

        public TogleButton()
        {   
            this.Click += TogleButton_Click;
        }

        void TogleButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IsChecked = !IsChecked;
        }
    }

    class ExtensionProperties
    {

    }
}
