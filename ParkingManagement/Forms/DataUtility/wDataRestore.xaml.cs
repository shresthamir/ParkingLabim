using ParkingManagement.Library;
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
using System.Windows.Shapes;
using Dapper;
namespace ParkingManagement.Forms.DataUtility
{
    /// <summary>
    /// Interaction logic for wDataRestore.xaml
    /// </summary>
    public partial class wDataRestore : Window
    {
        public wDataRestore()
        {
            InitializeComponent();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn.Content.ToString() == "...")
            {
                System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
                ofd.ShowDialog();
                if (!string.IsNullOrEmpty(ofd.FileName))
                    txtFile.Text = ofd.FileName;
            }
            else if (btn.Content.ToString() == "Restore")
            {
                if (MessageBox.Show("Once you restore the database the replaced record couldn't be recovered. Do you want to Continue?" + Environment.NewLine + "To restore database all connections must be closed.", "Data Restore", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (DataRestore(txtFile.Text, true, rbTranLog.IsChecked.Value))
                    {
                        GlobalClass.SetUserActivityLog("Database Restore", "Restore", string.Empty, string.Empty, string.Empty);                        
                        this.Close();
                    }
                }

            }
            else
            {
                this.Close();
            }
        }

        public bool DataRestore(string BackupFile, bool Data = true, bool TransactionLog = false)
        {
            string DataName, LogName, DataPath, LogPath, DBName;
            try
            {
                DataName = LogPath = LogName = DataPath = string.Empty;
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    conn.Open();
                    DBName = conn.Database;
                    conn.ChangeDatabase("Master");

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        IEnumerable<string> pids = conn.Query<string>("SELECT CAST(SPID AS VARCHAR(3)) FROM SYSPROCESSES S JOIN SYSDATABASES D ON S.dbid = D.dbid WHERE D.name = '" + DBName + "'");
                        foreach (string spid in pids)
                        {
                            conn.Execute("KILL " + spid);
                        }
                        if (Data)
                        {
                            cmd.CommandText = "RESTORE FILELISTONLY FROM Disk='" + BackupFile + "'";
                            SqlDataReader dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                if (dr["Type"].ToString() == "D")
                                {
                                    DataName = dr["LogicalName"].ToString();
                                    DataPath = dr["PhysicalName"].ToString();
                                    DataPath = DataPath.Replace(DataPath.Split('\\').Last(), string.Empty);
                                }
                                else
                                {
                                    LogName = dr["LogicalName"].ToString();
                                    LogPath = dr["PhysicalName"].ToString();
                                    LogPath = LogPath.Replace(LogPath.Split('\\').Last(), string.Empty);
                                }
                            }
                            dr.Close();

                            cmd.CommandText = @"RESTORE DATABASE " + DBName + " FROM Disk='" + BackupFile + @"' WITH REPLACE,
                                                     MOVE '" + DataName + "' TO '" + DataPath + DBName + @".mdf',
                                                    MOVE '" + LogName + "' TO '" + LogPath + DBName + ".ldf';";
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            cmd.CommandText = "RESTORE LOG " + DBName + " FROM DISK = '" + BackupFile + "' WITH REPLACE";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                MessageBox.Show("Database restore successfully completed.", "Data Restore", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(GlobalClass.GetRootException(ex).Message, "Data Restore", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
