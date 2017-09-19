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
    public partial class ResetPassword : Window
    {
        //ProductMasterServiceSoapClient proxy;
        public ResetPassword()
        {
            InitializeComponent();
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = "SELECT UID, UserName FROM USERS";
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (!GlobalClass.User.UID.Equals(dr["UID"]))
                                cmbUsers.Items.Add(new { UID = dr["UID"], UserName = dr["UserName"] });
                        }
                        dr.Close();
                    }
                    cmbUsers.DisplayMemberPath = "UserName";
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message, "Reset Password", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessageBox.Show("You are going to reset password for user : " + cmbUsers.Text +". Do you want to proceed?", "Reset Password", MessageBoxButton.YesNo, MessageBoxImage.Question)!= MessageBoxResult.Yes)
                {                    
                    return;
                }
                if(txtResetCode.Password!= "Ims16877Nepal")
                {
                    MessageBox.Show("Invalid Reset Code", "Change Password", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "ALTER LOGIN [" + cmbUsers.Text + "] WITH PASSWORD = ''";
                        cmd.ExecuteNonQuery();
                    }
                }
                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                {
                    SqlCommand cmdSave;
                    conn.Open();
                    cmdSave = new SqlCommand();
                    cmdSave.Connection = conn;
                    string _Salt = string.Empty;

                    cmdSave.CommandText = string.Format("UPDATE Users SET [Password]='{0}', SALT = '{1}' WHERE UserName = '{2}'", GlobalClass.GetEncryptedPWD("",ref _Salt), _Salt, cmbUsers.Text);
                    cmdSave.ExecuteNonQuery();
                    MessageBox.Show("Password Reset successfully!", "Change Password", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Change Password", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }


        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
