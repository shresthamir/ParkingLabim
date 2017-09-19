
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFCustomToolKit;

namespace ParkingManagement.Library.Controls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    partial class ucNumPad : UserControl
    {

        public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached("Target", typeof(UIElement), typeof(ucNumPad), new FrameworkPropertyMetadata());
        public ucNumPad()
        {
            InitializeComponent();
        }

        public static void SetValue(UIElement element, object value)
        {
            element.SetValue(TargetProperty, value);
        }
        public static object GetValue(UIElement element)
        {
            return element.GetValue(TargetProperty);
        }

        public ExtendedTextBox Target
        {
            get { return (ExtendedTextBox)GetValue(TargetProperty); }
            set
            {
                SetValue(TargetProperty, value);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string Text = (sender as Button).Content.ToString();

            Target.Focus();
            Target.SelectionStart = Target.Text.Length;
            if(!Target.IsFocused)
            {
                Target.IsDropDownOpen = false;
                return;
            }

            if (Text == "Back")
            {
                SendKey(Key.Back);
            }
            else if (Text == "Clear")
            {
                if (Target != null)
                    Target.Text = "";
                else
                    SendKey(Key.Clear);
            }
            else if (Text == "Done")
            {
                SendKey(Key.Enter);
                SendKey(Key.Tab);
                Target.IsDropDownOpen = false;
            }
            else
                SendInput(Text);

        }

        public void SendInput(string Text)
        {
            var eventArgs = new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, Keyboard.FocusedElement, Text))
            {
                RoutedEvent = TextInputEvent
            };
            InputManager.Current.ProcessInput(eventArgs);
        }
        public void SendKey(Key key)
        {
            // key = Key.Back;
            if (Keyboard.PrimaryDevice != null)
            {

                if (Keyboard.PrimaryDevice.ActiveSource != null)
                {
                    var e = new KeyEventArgs(Keyboard.PrimaryDevice, Keyboard.PrimaryDevice.ActiveSource, 0, key)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };
                    InputManager.Current.ProcessInput(e);
                }
            }

        }
    }
}
