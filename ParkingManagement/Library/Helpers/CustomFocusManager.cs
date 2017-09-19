using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
//using ;
namespace ParkingManagement.Library.Helpers
{
    public static class CustomFocusManager
    {

        public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(CustomFocusManager), new UIPropertyMetadata(false, FocusChanged));

        private static void FocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fe = (FrameworkElement)d;
            if(e.OldValue == null)
            {
                fe.GotFocus+=fe_GotFocus;
                fe.LostFocus += fe_LostFocus;
            }
            if(!fe.IsVisible)
            {
                fe.IsVisibleChanged += new DependencyPropertyChangedEventHandler(fe_IsVisibleChanged);
            }
            if((bool)e.NewValue)
            {
                fe.IsEnabled = true;
              System.Windows.Input.Keyboard.Focus(fe);       
            }
        }

        private static void fe_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            if(fe.IsVisible && (bool)((FrameworkElement)sender).GetValue(IsFocusedProperty))
            {
                fe.IsVisibleChanged -= fe_IsVisibleChanged;
                fe.Focus();
            }
        }

        private static void fe_GotFocus(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).SetValue(IsFocusedProperty, true);
        }

        private static void fe_LostFocus(object sender, RoutedEventArgs e)
        {
            ((FrameworkElement)sender).SetValue(IsFocusedProperty, false);
        }

        public static void SetIsFocused(DependencyObject element, bool value)
        {
            element.SetValue(IsFocusedProperty, value);

        }

        public static bool GetIsFocused(DependencyObject element)
        {
            return (bool)element.GetValue(IsFocusedProperty);
        }
    }
}
