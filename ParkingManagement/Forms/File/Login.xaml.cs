using ParkingManagement.Library;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using Dapper;
using System.Runtime.InteropServices;
using System.Data;
namespace ParkingManagement.Forms.File
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    /// 
    public partial class Login : Window
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetLocalTime(ref SYSTEMTIME st);


        public Login()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    MessageBox.Show(ex.InnerException.Message);
                else
                    MessageBox.Show(ex.Message);
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUserName.Text))
            {
                MessageBox.Show("Login ID cannot be empty!", "Login", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    var user = conn.Query<User>(string.Format("SELECT UID, UserName, [Password], FullName, UserCat, [STATUS], DESKTOP_ACCESS, MOBILE_ACCESS, SALT  FROM USERS WHERE UserName = '{0}'", txtUserName.Text)).FirstOrDefault();
                    if (user == null)
                    {
                        MessageBox.Show("Invalid username or password.", "Invalid Credential", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    if (user.Password != GlobalClass.GetEncryptedPWD(txtPassword.Password, ref user._Salt))
                    {
                        MessageBox.Show("Invalid username or password.", "Invalid Credential", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    if (user.DESKTOP_ACCESS != 1)
                    {
                        MessageBox.Show("You do not have privilage to access this application", "Insufficient Access", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    if (user.STATUS != 0)
                    {
                        MessageBox.Show("You no longer have privilage to access this application", "Insufficient Access", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    //using (SqlCommand cmd = conn.CreateCommand())
                    //{
                    //    conn.Open();
                    //    cmd.CommandText = "SP_CREATE_USER";
                    //    cmd.CommandType = CommandType.StoredProcedure;
                    //    cmd.Parameters.AddWithValue("@UNAME", txtUserName.Text);
                    //    cmd.Parameters.AddWithValue("@PWD", txtPassword.Password);
                    //    cmd.ExecuteNonQuery();
                    //}
                    user.DBPassword = txtPassword.Password;
                    GlobalClass.User = user;
                    if (!GlobalClass.StartSession())
                    {
                        MessageBox.Show("Session could not be started. Restart the application and try again.", "Login", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    var curDate = conn.ExecuteScalar<DateTime>("SELECT GETDATE()");
                    // MessageBox.Show(curDate.ToString("MM/dd/yyyy hh:mm tt"));
                    if (curDate.Subtract(DateTime.Now) > new TimeSpan(0, 0, 5) || DateTime.Now.Subtract(curDate) > new TimeSpan(0, 0, 5))
                        SetSystemTime(curDate);
                    new MainWindow().Show();
                    this.Close();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SetSystemTime(DateTime SERVERDATE)
        {
            SYSTEMTIME st = new SYSTEMTIME
            {
                wYear = short.Parse(SERVERDATE.Year.ToString()),
                wMonth = short.Parse(SERVERDATE.Month.ToString()),
                wDay = short.Parse(SERVERDATE.Day.ToString()),
                wDayOfWeek = (short)SERVERDATE.DayOfWeek,
                wHour = short.Parse(SERVERDATE.Hour.ToString()),
                wMinute = short.Parse(SERVERDATE.Minute.ToString()),
                wSecond = short.Parse(SERVERDATE.Second.ToString()),
                wMilliseconds = short.Parse(SERVERDATE.Millisecond.ToString())
            };
            if (!SetLocalTime(ref st))
                MessageBox.Show("Could not sync system date with server date", "Login", MessageBoxButton.OK);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Application.Current.Shutdown();
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectionStart = 0;
            ((TextBox)sender).SelectionLength = ((TextBox)sender).Text.Length;
        }

        private void txtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            ((PasswordBox)sender).SelectAll();

        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TraversalRequest tRequest = new TraversalRequest(FocusNavigationDirection.Next);
                UIElement keyboardFocus = Keyboard.FocusedElement as UIElement;

                if (keyboardFocus != null)
                {
                    keyboardFocus.MoveFocus(tRequest);
                }
                e.Handled = true;
            }

        }

        private void etbPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtPassword.Password = etbPassword.Text;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEMTIME
    {
        public short wYear;
        public short wMonth;
        public short wDayOfWeek;
        public short wDay;
        public short wHour;
        public short wMinute;
        public short wSecond;
        public short wMilliseconds;
    }
}
