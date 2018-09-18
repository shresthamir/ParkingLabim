using ParkingManagement.Library;
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
namespace ParkingManagement.Forms.File
{
    /// <summary>
    /// Interaction logic for ChangePassword.xaml
    /// </summary>
    public partial class ChangePassword : Window
    {
        //ProductMasterServiceSoapClient proxy;
        public ChangePassword()
        {
            
            InitializeComponent();
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (txtNewPass.Password != txtCPass.Password)
            {
                MessageBox.Show("New Password and Confirm Password doesn't matched","Change Password",MessageBoxButton.OK,MessageBoxImage.Exclamation);
                return;
            }
            else
            {
                 try
                {
                    if (GlobalClass.User.Password != GlobalClass.GetEncryptedPWD(txtOldPass.Password,ref GlobalClass.User._Salt))
                    {
                        MessageBox.Show("Password doesn't matched","Change Password",MessageBoxButton.OK,MessageBoxImage.Exclamation);
                        return;
                    }
                    //using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                    //{
                    //    conn.Open();
                    //    using (SqlCommand cmd = conn.CreateCommand())
                    //    {
                    //        GlobalClass.User.DBPassword = txtNewPass.Password;
                    //        cmd.CommandText = "ALTER LOGIN [" + GlobalClass.User.UserName + "] WITH PASSWORD = '" + GlobalClass.User.DBPassword + "'";
                    //        cmd.ExecuteNonQuery();                            
                    //    }
                    //}
                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    {
                        SqlCommand cmdSave;
                        conn.Open();
                        cmdSave = new SqlCommand();
                        cmdSave.Connection = conn;
                        GlobalClass.User._Salt = string.Empty;

                        cmdSave.CommandText = string.Format("UPDATE Users SET [Password]='{0}', SALT = '{1}' WHERE UID = {2}", GlobalClass.GetEncryptedPWD(txtNewPass.Password, ref GlobalClass.User._Salt), GlobalClass.User.SALT, GlobalClass.User.UID);
                        cmdSave.ExecuteNonQuery();
                        MessageBox.Show("Password Changed successfully!", "Change Password", MessageBoxButton.OK, MessageBoxImage.Information);
                        GlobalClass.User.Password = GlobalClass.GetEncryptedPWD(txtNewPass.Password, ref GlobalClass.User._Salt);

                        this.Close();
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message,"Change Password",MessageBoxButton.OK,MessageBoxImage.Error);
                }

            }
        }
        

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
