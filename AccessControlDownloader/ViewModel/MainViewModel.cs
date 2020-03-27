using Dapper;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace AccessControlDownloader.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propname)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propname));
            }
        }

        public RelayCommand ReadLogDateCommand { get; set; }
        public ObservableCollection<Device> DeviceList { get { return _Device; } set { _Device = value; OnPropertyChanged("DeviceList"); } }
        public DateTime DateToday { get { return _DateToday; } set { _DateToday = value; OnPropertyChanged("DateToday"); } }

        public MainViewModel()
        {
            ReadLogDateCommand = new RelayCommand(ExecuteReadLog);
            GetAdminUser();
            GetDeviceList();
            //CheckDeviceStatus();
            StartTimer();
        }

        private void CheckDeviceStatus()
        {
            foreach (var device in DeviceList)
            {
                var zkem = new zkemkeeper.CZKEM();
                if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                {
                    //device.Status = true;
                    device.GridBackground = new SolidColorBrush(Color.FromArgb(255, 255, 139, 0));
                }
                else
                {
                    //device.Status = false;
                    device.GridBackground = new SolidColorBrush(Color.FromArgb(255, 255, 109, 0));

                }
            }
        }

        public void StartTimer()
        {
            DispatcherTimer dispatchTimer = new DispatcherTimer();
            dispatchTimer.Tick += new EventHandler(DispatchTime_Tick);
            dispatchTimer.Interval = new TimeSpan(0, 0, 1);
            dispatchTimer.Start();
        }

        private int timerSec = 0;
        private async void DispatchTime_Tick(object sender, EventArgs e)
        {
            try
            {
                //var idleTime = IdleTimeDetector.GetIdleTimeInfo();
                //if (idleTime.IdleTime.TotalSeconds >= 100)
                //{

                //}

                if (timerSec == 60)
                {
                    ExecuteReadLog(null);
                    timerSec = 0;
                }
                timerSec++;
                DateToday = DateTime.Now;
                //CheckDeviceStatus();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }

        private void GetDeviceList()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                DeviceList = new ObservableCollection<Device>(conn.Query<Device>("select  * from DeviceList D join vehicletype V on D.vehicletype=V.vtypeid"));
                foreach (Device vtype in DeviceList)
                {
                    if (vtype.ButtonImage == null)
                    {
                        continue;
                    }

                    vtype.ImageSource = Imaging.BinaryToImage(vtype.ButtonImage);
                }
            }
        }
        private void GetAdminUser()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.DataConnectionString))
            {
                var user = conn.Query<User>(string.Format("SELECT UID, UserName, [Password], FullName, UserCat, [STATUS], DESKTOP_ACCESS, MOBILE_ACCESS, SALT  FROM USERS WHERE UserName = 'admin'")).FirstOrDefault();
                if (user == null)
                {
                    MessageBox.Show("Admin user not found!.", "Invalid Credential", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                GlobalClass.User = user;
            }
        }

        public int dwTMachineNumber = 0;
        public string dwEnrollNumber = "";
        public int dwEnrollNumberInt = 0;
        public int dwEMachineNumber = 0;
        public int dwVerifyMode = 0;
        public int dwInOutMode = 0;
        public int dwYear = 0;
        public int dwMonth = 0;
        public int dwDay = 0;
        public int dwHour = 0;
        public int dwMinute = 0;
        public int dwSecond = 0;
        public int dwWorkCode = 0;
        private ObservableCollection<Device> _Device;
        private DateTime _DateToday;

        private void ExecuteReadLog(object obj)
        {
            try
            {
                foreach (var device in DeviceList)
                {
                    var zkem = new zkemkeeper.CZKEM();
                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    {
                        var totalEnteredVechicle = conn.ExecuteScalar<int>($"SELECT count(*) FROM PARKINGINDETAILS p join devicelist d on p.vehicletype=d.vehicletype  where deviceid='{device.DeviceId}' and convert(date, indate) = convert(date, getdate())");
                        DeviceList.Where(x => x.VehicleType == device.VehicleType).ToList().ForEach(x => x.EnteredVehicle = totalEnteredVechicle);
                    }


                    if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                    {
                        device.Status = true;
                        device.GridBackground = new SolidColorBrush(Colors.LightGreen);


                        if (zkem.ReadAllGLogData(zkem.MachineNumber))
                        {
                            MainViewModel ld = new MainViewModel();

                            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                            {
                                conn.Open();
                                using (SqlTransaction tran = conn.BeginTransaction())
                                {
                                    while (zkem.GetGeneralLogData(zkem.MachineNumber, ref ld.dwTMachineNumber, ref ld.dwEnrollNumberInt, ref ld.dwEMachineNumber, ref ld.dwVerifyMode, ref ld.dwInOutMode, ref ld.dwYear, ref ld.dwMonth, ref ld.dwDay, ref ld.dwHour, ref ld.dwMinute))
                                    {
                                        if (ld.dwEnrollNumberInt > short.MaxValue || ld.dwEnrollNumberInt < short.MinValue)
                                        {
                                            continue;
                                        }

                                        SaveLogsToDb(ld, tran, device);
                                        DeActivateEnteredCard(ld.dwEnrollNumberInt);

                                    }
                                    tran.Commit();
                                    zkem.ClearGLog(zkem.MachineNumber);
                                }
                            }
                        }
                    }
                    else
                    {
                        device.Status = false;
                        device.GridBackground = new SolidColorBrush(Colors.Red);

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "AccessControlSync", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
        private void SaveLogsToDb(MainViewModel ld, SqlTransaction tran, Device device)
        {
            ParkingIn Parking = new ParkingIn();

            var maxcardId = GetMaxCardId();
            if (ld.dwEnrollNumberInt <= maxcardId)
            {
                Parking.PID = Convert.ToInt32(GetInvoiceNo("PID", tran));
                Parking.FYID = GlobalClass.FYID;
                Parking.Barcode = GetCardNumberByEnrollId(ld.dwEnrollNumberInt);
                Parking.UID = GlobalClass.User.UID;
                //Parking.PlateNo = ld.dwEnrollNumber;
                Parking.SESSION_ID = 1;
                var logDate = new DateTime(ld.dwYear, ld.dwMonth, ld.dwDay, ld.dwHour, ld.dwMinute, ld.dwSecond);
                Parking.InDate = new DateTime(ld.dwYear, ld.dwMonth, ld.dwDay);
                Parking.InTime = logDate.ToString("HH:mm:ss");
                var nepDate = new DateConverter(GlobalClass.TConnectionString);
                Parking.InMiti = nepDate.CBSDate(Parking.InDate);
                Parking.VehicleType = GetVechicleTypeByDeviceId(device.DeviceId);

                var alreadyExist = tran.Connection.QueryFirstOrDefault<ParkingIn>($"Select * from parkingindetails where fyid=@fyid and vehicletype=@vehicletype and indate=@indate and inmiti=@inmiti and intime=@intime", Parking, tran);
                if (alreadyExist == null)
                {
                    string strSQL = @"INSERT INTO ParkingInDetails(PID, FYID, VehicleType, InDate, InTime, PlateNo, Barcode, [UID], InMiti, SESSION_ID)
                          Values(@PID, @FYID, @VehicleType,@InDate,@InTime,@PlateNo,@Barcode,@UID,@InMiti, @SESSION_ID)";
                    tran.Connection.Execute(strSQL, Parking, tran);

                    tran.Connection.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = 'PID' AND FYID = " + GlobalClass.FYID, transaction: tran);
                }

            }
            else
            {
                //MessageBox.Show($"CardId: {ld.dwEnrollNumberInt} is not present in database!");
            }
        }

        private void DeActivateEnteredCard(int dwEnrollNumberInt)
        {
           //TODO: Disable enrolled card from access control device
        }

        private int GetMaxCardId()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                int cardid = conn.QueryFirstOrDefault<int>($"select max(isnull(cardid,0)) from dailycards");
                return cardid;
            }
        }

        private byte GetVechicleTypeByDeviceId(int deviceId)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                byte vehicleType = conn.QueryFirstOrDefault<byte>($"select vehicletype from DeviceList where deviceid='{deviceId}'");
                return vehicleType;
            }
        }

        private string GetCardNumberByEnrollId(int dwEnrollNumberInt)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                string cardNumber = conn.QueryFirstOrDefault<string>($"select cardnumber from dailycards where cardid='{dwEnrollNumberInt}'");
                return cardNumber;
            }
        }

        string GetInvoiceNo(string VNAME, SqlTransaction tran)
        {
            string invoice = tran.Connection.ExecuteScalar<string>("SELECT CurNo FROM tblSequence WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = VNAME, FYID = GlobalClass.FYID }, tran);
            if (string.IsNullOrEmpty(invoice))
            {
                tran.Connection.Execute("INSERT INTO tblSequence(VNAME, FYID, CurNo) VALUES(@VNAME, @FYID, 1)", new { VNAME = VNAME, FYID = GlobalClass.FYID }, tran);
                invoice = "1";
            }
            return invoice;
        }
    }
}
