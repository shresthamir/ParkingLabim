using Dapper;
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
namespace GateApplication.Forms.File
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        //private ApiHelper _apiHelper;

        public Login()
        {
            try
            {

                InitializeComponent();
                //_apiHelper = new ApiHelper();

                //this.txtUserName.Focus();
                //using(TicketBoxDataContext dc = new TicketBoxDataContext())
                //{
                //    var spoint = dc.SPoint.FirstOrDefault(x => x.Name == GlobalClass.TerminalName);
                //    //MessageBox.Show(GetProcessorId());
                //    spoint.SChannel = dc.SChannel.FirstOrDefault(x => x.SCID == spoint.SCID);
                //    GlobalClass.Terminal = spoint;
                //    //if (spoint.DeviceID== GetProcessorId())
                //    //{
                //    //    spoint.SChannel = dc.SChannel.FirstOrDefault(x => x.SCID == spoint.SCID);
                //    //    GlobalClass.Terminal = spoint;

                //    //}
                //    //else
                //    //{
                //    //    MessageBox.Show("Sales point not registered", "Check Registration", MessageBoxButton.OK, MessageBoxImage.Error);
                //    //    Application.Current.Shutdown();

                //    //}
                //}
            }
            catch (Exception ex)
            {
                //GlobalClass.ProcessError(ex, "Login");
            }

        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtUserName.Text))
                {
                    MessageBox.Show("Login ID cannot be empty!", "Login", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
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
                    //if (user.DESKTOP_ACCESS != 1)
                    //{
                    //    MessageBox.Show("You do not have privilage to access this application", "Insufficient Access", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    //    return;
                    //}
                    //if (user.STATUS != 0)
                    //{
                    //    MessageBox.Show("You no longer have privilage to access this application", "Insufficient Access", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    //    return;
                    //}
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
                    //if (!GlobalClass.StartSession())
                    //{
                    //    MessageBox.Show("Session could not be started. Restart the application and try again.", "Login", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    //    return;
                    //}
                    //var curDate = conn.ExecuteScalar<DateTime>("SELECT GETDATE()");
                    // MessageBox.Show(curDate.ToString("MM/dd/yyyy hh:mm tt"));
                    //if (curDate.Subtract(DateTime.Now) > new TimeSpan(0, 0, 5) || DateTime.Now.Subtract(curDate) > new TimeSpan(0, 0, 5))
                    //    SetSystemTime(curDate);
                    new MainWindow().Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Login", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                btnLogin_Click(this, null);
        }

        private string GetProcessorId()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                System.Management.SelectQuery theClass = new System.Management.SelectQuery("Win32_Processor");

                using (System.Management.ManagementObjectSearcher theCollectionOfResults = new System.Management.ManagementObjectSearcher(theClass))
                {
                    foreach (System.Management.ManagementObject currentResult in theCollectionOfResults.Get())
                    {
                        if (currentResult["ProcessorID"] != null)
                        {
                            sb.Append(currentResult["ProcessorID"].ToString());
                            break;
                        }
                    }
                }
                var Class = new System.Management.ManagementClass("Win32_NetworkAdapter");//Win32_BIOS

                using (System.Management.ManagementObjectCollection theCollectionOfResults = Class.GetInstances())
                {
                    foreach (System.Management.ManagementObject currentResult in theCollectionOfResults)
                    {
                        if (currentResult["MacAddress"] != null)
                        {
                            sb.Append(currentResult["MacAddress"].ToString().Replace(":", string.Empty));
                            break;
                        }
                    }
                }

                return sb.ToString();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return "";
            }
        }
    }
}
