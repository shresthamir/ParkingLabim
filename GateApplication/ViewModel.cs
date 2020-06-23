using Dapper;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Threading;
namespace GateApplication
{
    public class GateEntryViewModel : BaseViewModel
    {
        #region members
        DispatcherTimer timer;
        string _barcode;
        string _Message;
        string _image;
        System.Windows.Media.Brush _MsgColor;
        string _EntryTime;
        private Visibility _TInfoVisible = Visibility.Collapsed;
        private string _BillNo;
        private ObservableCollection<Voucher> _TicketList;



        #endregion

        #region Property
        public string Barcode
        {
            get { return _barcode; }
            set { _barcode = value; OnPropertyChanged("BarCode"); }
        }
        public string Message
        {
            get { return _Message; }
            set { _Message = value; OnPropertyChanged("Message"); }
        }
        public string Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged("Image"); }
        }
        public string EntryTime
        {
            get { return _EntryTime; }
            set { _EntryTime = value; OnPropertyChanged("EntryTime"); }
        }
        public string BillNo { get { return _BillNo; } set { _BillNo = value; OnPropertyChanged("BillNo"); } }


        public ObservableCollection<Voucher> TicketList { get { return _TicketList; } set { _TicketList = value; OnPropertyChanged("TicketList"); } }
        public Visibility TInfoVisible { get { return _TInfoVisible; } set { _TInfoVisible = value; OnPropertyChanged("TInfoVisible"); } }
        public System.Windows.Media.Brush MsgColor
        {
            get { return _MsgColor; }
            set { _MsgColor = value; OnPropertyChanged("MsgColor"); }
        }
        #endregion

        #region Construction
        public GateEntryViewModel()
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 3);
            timer.IsEnabled = false;
            timer.Tick += timer_Tick;
            TicketList = new ObservableCollection<Voucher>();
            LoadData = new RelayCommand(ExecuteCheckTicket);
        }

        private bool canExecuteLoad(object obj)
        {
            return TInfoVisible == Visibility.Collapsed;
        }
        #endregion

        #region Methods
        void ExecuteCheckTicket(object param)
        {
            if (AuthenticateTicket(param.ToString()))
            {
                Image = "../Images/OK.png";
                MsgColor = System.Windows.Media.Brushes.Green;
            }
            else
            {
                Image = "../Images/Close.png";
                MsgColor = System.Windows.Media.Brushes.Red;
            }
            Barcode = string.Empty;
            timer.IsEnabled = true;
        }

        public bool AuthenticateTicket(string barcode)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {

                        string sql = "Select * from parkingvouchers where barcode=@barcode";
                        var res = conn.QueryFirstOrDefault<Voucher>(sql, new { barcode }, tran);
                        if (res == null)
                        {
                            Message = "Ticket Not Found!";
                            return false;
                        }
                        if (res.IsUsed == 1)
                        {
                            Message = "Ticket Already Used!";
                            return false;
                        }

                        TicketList.Add(res);

                        string sqlLog = @"INSERT INTO TicketScanLog (EntryNo,TDate,Barcode,[uname],[TTIME])
                                                             VALUES
                                                                   ((SELECT ISNULL(MAX(EntryNo),0) + 1 FROM TicketScanLog),getdate(),@barcode,'" + GlobalClass.User.UserName + "',(SELECT CONVERT(VARCHAR,(SELECT GETDate()),8)))";
                        var resLog = conn.Execute(sqlLog, new { barcode }, tran);
                        if (resLog > 0)
                        {
                            string sqlUpdate = "update ParkingVouchers set isused=1 where Barcode=@barcode";
                            var resUpdate = conn.Execute(sqlUpdate, new { barcode }, tran);
                            tran.Commit();
                            if (resUpdate > 0)
                            {
                                Message = "Authentication Successfull";
                                return true;
                            }
                        }

                        tran.Rollback();
                        Message = "Authentication Failed";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "AuthenticateTicket", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Image = string.Empty;
            Message = string.Empty;
            MsgColor = System.Windows.Media.Brushes.Red;
            TInfoVisible = Visibility.Collapsed;
            timer.IsEnabled = false;
        }
        #endregion
    }
}
