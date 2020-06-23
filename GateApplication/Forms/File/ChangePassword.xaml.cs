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
    /// Interaction logic for ChangePassword.xaml
    /// </summary>
    public partial class ChangePassword : Window
    {
        //public UserAuthorityService UserAuthorityService { get; private set; }

        public ChangePassword()
        {

            InitializeComponent();
            //UserAuthorityService = new UserAuthorityService();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //if (GlobalClass.User.PWD != GlobalClass.GetEncryptedPWD(txtOldPass.Password))
            //{
            //    MessageBox.Show("Incorrect Password", "Change Password", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            //    return;
            //}
            if (txtNewPass.Password == txtCPass.Password)
            {
                try
                {
                    //var res = await UserAuthorityService.ChangePassword(GlobalClass.LoggedInUser.username, txtOldPass.Password, txtNewPass.Password);
                    //if (res.status == "ok")
                    //{
                    //    MessageBox.Show("Password successfully changed.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Information);
                    //}
                    //else
                    //{
                    //    MessageBox.Show(res.result.ToString() + res.Ex.ToString(), "Change Password", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    //}
                    this.Close();
                    //using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                    //using (SqlCommand cmd = new SqlCommand())
                    //{
                    //    conn.Open();
                    //    cmd.Connection = conn;
                    //    cmd.CommandText = "UPDATE tblUsers SET [PWD] = '" + GlobalClass.GetEncryptedPWD(txtNewPass.Password) + "' WHERE UID=" + GlobalClass.User.UID;
                    //    cmd.ExecuteNonQuery();
                    //    GlobalClass.User.PWD = GlobalClass.GetEncryptedPWD(txtNewPass.Password);
                    //    MessageBox.Show("Password successfully changed.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Information);
                    //    this.Close();
                    //}
                }
                catch (Exception ex)
                {
                    //GlobalClass.ProcessError(ex, "Change Password");
                }
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
