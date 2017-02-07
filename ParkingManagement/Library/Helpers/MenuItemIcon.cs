using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ParkingManagement.Library.Helpers
{
    class MenuItemIcon
    {

        public static readonly DependencyProperty IconImageProperty = DependencyProperty.RegisterAttached("IconImage", typeof(ImageSource), typeof(MenuItemIcon), new PropertyMetadata(new BitmapImage(), IconImageChanged));


        public static ImageSource GetIconImage(DependencyObject obj)
        {
            return (ImageSource)obj.GetValue(IconImageProperty);
        }

        // Set
        public static void SetIconImage(DependencyObject obj, ImageSource value)
        {
            obj.SetValue(IconImageProperty, value);
        }
        public static void IconImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MenuItem mi  = (MenuItem)d;
            Image Icon =  new Image();            
            Icon.Source = e.NewValue as ImageSource;
            mi.Icon = Icon;
        }
    }
}
