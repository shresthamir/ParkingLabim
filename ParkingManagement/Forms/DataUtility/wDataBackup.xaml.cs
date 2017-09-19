using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;
using System.Data.SqlClient;
using ParkingManagement.Library;
namespace ParkingManagement.Forms.DataUtility
{
    /// <summary>
    /// Interaction logic for wDataRestore.xaml
    /// </summary>
    public partial class wDataBackup : Window
    {
        public wDataBackup()
        {
            InitializeComponent();
            rbFullBackup.IsChecked = true;            
            txtDirectory.Text = Environment.CurrentDirectory;
            txtFile.Text = "ParkingData - " + DateTime.Today.ToString("MM-dd-yy") + "";
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn.Content.ToString() == "...")
            {

                System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();
                ofd.ShowDialog();
                if (!string.IsNullOrEmpty(ofd.SelectedPath))
                    txtDirectory.Text = ofd.SelectedPath;
            }
            else if (btn.Content.ToString() == "Backup")
            {
                if (!Directory.Exists(txtDirectory.Text))
                {
                    MessageBox.Show("Selected Directory does not Exists. Please select valid directory and try again", "Data Backup", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    txtDirectory.Focus();
                    return;
                }
                if (System.IO.File.Exists(txtDirectory.Text + "\\" + txtFile.Text + txtExt.Text))
                {
                    if (MessageBox.Show("A Backup file with same name already Exists. Would you like to overwrite old backup with new one?", "Data Backup", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        System.IO.File.Delete(txtDirectory.Text + "\\" + txtFile.Text + txtExt.Text);
                    else
                    {
                        txtFile.Focus();
                        return;
                    }
                }

                if (ManualDataBackup(txtDirectory.Text + "\\" + txtFile.Text + txtExt.Text, rbFullBackup.IsChecked.Value, rbTranLog.IsChecked.Value))
                {
                    GlobalClass.SetUserActivityLog("Database Backup", "Backup", string.Empty, string.Empty, string.Empty);                    
                    this.Close();
                }               
            }
            else
            {
                this.Close();
            }
        }

        private void rbFullBackup_Checked(object sender, RoutedEventArgs e)
        {
            txtFile.Text = "ParkingData - " + DateTime.Today.ToString("MM-dd-yy");
            txtExt.Text = ".Bak";
        }

        private void rbTranLog_Checked(object sender, RoutedEventArgs e)
        {
            txtFile.Text = "ParkingLog - " + DateTime.Today.ToString("MM-dd-yy");
            txtExt.Text = ".Trn";
        }

        bool ManualDataBackup(string BackupPath, bool Data = true, bool TransactionLog = false)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    if (Data)
                    {
                        cmd.CommandText = string.Format("Backup Database {0} TO DISK = '{1}'", conn.Database, BackupPath);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Database backup successfully completed.", "Data Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                        return true; 
                    }
                    else
                    {
                        cmd.CommandText = string.Format("Backup Log {0} TO DISK = '{1}'", conn.Database, BackupPath);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Transaction Log backup successfully completed.", "Data Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Data Backup", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
