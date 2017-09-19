using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ParkingManagement.Library.Helpers
{
    public static class ControlFinder
    {
        public static T FindFirstVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T childOfChild = FindFirstVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        public static T FindAncestor<T>(DependencyObject obj) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            if (parent != null && parent is T)
            {
                return (T)parent;
            }
            else
            {
                T parentOfParent = FindAncestor<T>(parent);
                if (parentOfParent != null)
                    return parentOfParent;
            }
            return null;
        }

        public static T FindAncestor<T>(DependencyObject obj, string Name) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            if (parent != null && parent is T && (parent as FrameworkElement).Name == Name)
            {
                return (T)parent;
            }
            else
            {
                T parentOfParent = FindAncestor<T>(parent, Name);
                if (parentOfParent != null)
                    return parentOfParent;
            }
            return null;
        }



    }
}
