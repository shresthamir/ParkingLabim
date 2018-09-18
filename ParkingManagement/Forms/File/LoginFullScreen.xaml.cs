using System;
using System.Collections.Generic;
using System.Data.SqlClient;
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
using System.Windows.Threading;
using Dapper;
using System.IO;
using ParkingManagement.Library;
using System.Runtime.InteropServices;

namespace ParkingManagement.Forms.File
{
    /// <summary>
    /// Interaction logic for ucLogin.xaml
    /// </summary>
    public partial class LoginFullScreen : Window
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetLocalTime(ref SYSTEMTIME st);
        public LoginFullScreen()
        {
            try
            {
                InitializeComponent();
                //this.WindowStyle = WindowStyle.None;
                //this.ResizeMode = ResizeMode.NoResize;
                
                if (System.IO.File.Exists(Environment.CurrentDirectory + "\\Background.jpg"))
                {
                    this.imgBackground.Source = Imaging.FileToImage(Environment.CurrentDirectory + "\\Background.jpg");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, "IMS - Ticketing Software", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Login_Loaded(object sender, RoutedEventArgs e)
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void txtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            ((PasswordBox)sender).SelectAll();
        }
        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Login_Click(this, null);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtpassword.Password))
                {
                    MessageBox.Show("PIN cannot be blank!", "Login", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                string EPassword = Encrypt(txtpassword.Password, "AmitLalJoshi");
                using (SqlConnection con = new SqlConnection(GlobalClass.TConnectionString))
                using (SqlCommand cmd = con.CreateCommand())
                {
                    con.Open();
                    if (Settings.GblPasswordWiseLogin == 1)

                        cmd.CommandText = @"SELECT U.UNAME, U.PASSWORD, U.ROLE, ISNULL(U.IsWaiter, 0) IsWaiter FROM USERPROFILES U WHERE PASSWORD = @PASSWORD";
                    else
                    {
                        cmd.CommandText = @"SELECT U.UNAME, U.PASSWORD, U.ROLE, ISNULL(U.IsWaiter, 0) IsWaiter FROM USERPROFILES U WHERE UNAME = @UNAME AND PASSWORD = @PASSWORD";
                        cmd.Parameters.AddWithValue("@UNAME", txtusername.Text);
                    }
                    cmd.Parameters.AddWithValue("@PASSWORD", EPassword);
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (dr.HasRows)
                    {
                        dr.Read();
                        GlobalClass.TRNUSER = dr["UNAME"].ToString();
                        GlobalClass.UserRole = dr["ROLE"].ToString();
                        GlobalClass.TRNUSERPWD = txtpassword.Password;
                        GlobalClass.IsUserWaiter = (byte)dr["IsWaiter"];
                        dr.Close();
                        loginsuccess();
                        ImportWaiter();
                    }
                    else
                    {
                        MessageBox.Show("Invalid Username or Password!", "Login", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalClass.ProcessError(ex, "Login Error");
            }
            finally
            {
                GC.Collect();
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

    }
}
