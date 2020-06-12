using AccessControlDownloader.Helper;
using Dapper;
using Newtonsoft.Json;
using NLog;
using ParkingManagement.Enums;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using zkemkeeper;
using ParkingManagement.Services;
using ParkingManagement.Dtos;

namespace AccessControlDownloader.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private static Logger MainLogger = LogManager.GetLogger("MainViewModel");
        public event PropertyChangedEventHandler PropertyChanged;
        //string clearHour = ConfigurationManager.AppSettings["clearHour"];
        //string clearMinute = ConfigurationManager.AppSettings["clearMinute"];

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
        public bool initialLoad { get; set; } = true;

        public MainViewModel()
        {
            ReadLogDateCommand = new RelayCommand(ExecuteReadLog);
            GetAdminUser();
            GetDeviceList();
            //CheckDeviceStatus();
            StartTimer();
            MainLogger.Info("Sync App Started...");
            //SyncDeviceDateAndClearDeviceData();
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
        private int timerToSyncDeviceTime = 0;
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
                    await SaveAccountBill();

                    timerSec = 0;
                }

                timerSec++;
                timerToSyncDeviceTime++;
                DateToday = DateTime.Now;



                //CheckDeviceStatus();

            }
            catch (Exception ex)
            {
                MainLogger.Error("From DispatchTime_Tick: " + ex.Message.ToString());
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

        private async void ExecuteReadLog(object obj)
        {
            try
            {
                // Skip logs of exit device 
                //foreach (var device in DeviceList.Where(x => x.DeviceType == (int)DeviceType.Entry))
                foreach (var device in DeviceList)
                {
                    var zkem = new zkemkeeper.CZKEM();

                    device.DeviceIp = device.DeviceIp.Trim();
                    bool isValidIpA = UniversalStatic.ValidateIP(device.DeviceIp);
                    if (!isValidIpA)
                    {
                        device.Status = false;
                        device.GridBackground = new SolidColorBrush(Colors.Red);
                        MainLogger.Error($"Invalid Ip: {device.DeviceIp}");
                        continue;
                        //throw new Exception("The Device IP is invalid !!");
                    }

                    isValidIpA = UniversalStatic.PingTheDevice(device.DeviceIp);
                    if (!isValidIpA)
                    {
                        device.Status = false;
                        device.GridBackground = new SolidColorBrush(Colors.Red);
                        MainLogger.Error($"Failed to ping {device.DeviceIp}");
                        continue;
                        //throw new Exception("The device at " + device.DeviceIp + ":" + device.DevicePort + " did not respond!!");
                    }

                    if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                    {
                        //MainLogger.Info($"Connected to Device sucessfully: {device.DeviceIp}");
                        device.Status = true;
                        device.GridBackground = new SolidColorBrush(Colors.LightGreen);
                        //List<MainViewModel> logData = new List<MainViewModel>();

                        ////To get device time every hour
                        if (initialLoad || timerToSyncDeviceTime == 3600)
                        {
                            initialLoad = false;
                            var deviceDate = GetDeviceDate(zkem);

                            if (deviceDate.ToString("g") != DateTime.Now.ToString("g"))
                            {
                                zkem.SetDeviceTime(zkem.MachineNumber);
                                MainLogger.Info($"System date and device found different. So Date time synced with device: {device.DeviceIp}");
                                deviceDate = GetDeviceDate(zkem);
                            }
                            MainLogger.Info($"Date time already in sync with device: {device.DeviceIp}");
                            device.DeviceTime = DateTime.Now.ToString("hh:mm tt"); //since device date is set with system date both are same
                            timerToSyncDeviceTime = 0;
                        }
                        if (!initialLoad)
                        {
                            device.DeviceTime = DateTime.Now.ToString("hh:mm tt"); //increment time every minute
                        }

                        if (zkem.ReadAllGLogData(zkem.MachineNumber))
                        {
                            //MainViewModel ld = new MainViewModel();

                            while (zkem.GetGeneralLogData(zkem.MachineNumber, ref dwTMachineNumber, ref dwEnrollNumberInt, ref dwEMachineNumber, ref dwVerifyMode, ref dwInOutMode, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute))
                            {
                                if (dwEnrollNumberInt > short.MaxValue || dwEnrollNumberInt < short.MinValue)
                                {
                                    continue;
                                }
                                //logData.Add(new MainViewModel
                                //{
                                //    dwTMachineNumber = ld.dwTMachineNumber,
                                //    dwEMachineNumber = ld.dwEMachineNumber,
                                //    dwEnrollNumberInt = ld.dwEnrollNumberInt,
                                //    dwVerifyMode = ld.dwVerifyMode,
                                //    dwInOutMode = ld.dwInOutMode,
                                //    dwYear = ld.dwYear,
                                //    dwMonth = ld.dwMonth,
                                //    dwDay = ld.dwDay,
                                //    dwHour = ld.dwHour,
                                //    dwMinute = ld.dwMinute
                                //});
                                if (device.DeviceType == (int)DeviceType.Entry)
                                {
                                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                                    {
                                        conn.Open();
                                        using (SqlTransaction tran = conn.BeginTransaction())
                                        {
                                            //SaveLogsToDb(this, tran, device);
                                            SaveToParkingInDetails(this, tran, device);
                                            if (CheckIfMemberCard(this.dwEnrollNumberInt, tran))
                                            {
                                                //To Deactivate member card
                                                zkem.EnableUser(zkem.MachineNumber, this.dwEnrollNumberInt, zkem.MachineNumber, 10, false);
                                            }
                                            tran.Commit();
                                        }
                                    }
                                }
                                else if (device.DeviceType == (int)DeviceType.Exit)
                                {
                                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                                    {
                                        conn.Open();
                                        using (SqlTransaction tran = conn.BeginTransaction())
                                        {
                                            var success = SaveToParkingOutDetails(this, tran, device);

                                            if (success)
                                            {
                                                //To reactivate card after exit
                                                zkem.EnableUser(zkem.MachineNumber, this.dwEnrollNumberInt, zkem.MachineNumber, 10, true);
                                            }
                                            tran.Commit();
                                        }
                                    }
                                }
                            }

                            //-------------------save to log before clearing data--------------
                            //var logs = logData.Select(x => new DeviceLog
                            //{
                            //    dwTMachineNumber = x.dwTMachineNumber,
                            //    dwEMachineNumber = x.dwEMachineNumber,
                            //    dwEnrollNumberInt = x.dwEnrollNumberInt,
                            //    dwVerifyMode = x.dwVerifyMode,
                            //    dwInOutMode = x.dwInOutMode,
                            //    dwYear = x.dwYear,
                            //    dwMonth = x.dwMonth,
                            //    dwDay = x.dwDay,
                            //    dwHour = x.dwHour,
                            //    dwMinute = ld.dwMinute,
                            //    DeviceIp = device.DeviceIp,
                            //    DeviceId = device.DeviceId
                            //});
                            //string jsonLogsObj = JsonConvert.SerializeObject(logs);
                            //MainLogger.Info(jsonLogsObj);
                            //-------------------save to log before clearing data--------------

                            //clear data of current device
                            var TimeToClear = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, Convert.ToInt32(GlobalClass.clearhour), Convert.ToInt32(GlobalClass.clearminute), 0);
                            if (DateTime.Now >= TimeToClear && DateTime.Now <= TimeToClear.AddMinutes(2))
                            {

                                zkem.EnableDevice(zkem.MachineNumber, false);
                                MainLogger.Info($"Disabled device to clear logs: {device.DeviceIp}");
                                zkem.ClearGLog(zkem.MachineNumber);
                                MainLogger.Info($"Cleared logs of device: {device.DeviceIp}");

                                zkem.EnableDevice(zkem.MachineNumber, true);
                                MainLogger.Info($"Enabled device after clrearing logs: {device.DeviceIp}");

                                DisableExpiredMember(zkem);
                            }

                            //using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                            //{
                            //    conn.Open();
                            //    using (SqlTransaction tran = conn.BeginTransaction())
                            //    {
                            //        foreach (var log in logData)
                            //        {
                            //            //// Skip logs of member device
                            //            //if (device.IsMemberDevice == 1)
                            //            //{
                            //            //    if (CheckIfMemberCard(ld.dwEnrollNumberInt, tran))
                            //            //    {
                            //            //        //To Deactivate member card
                            //            //        zkem.EnableUser(zkem.MachineNumber, ld.dwEnrollNumberInt, zkem.MachineNumber, 10, false);
                            //            //    }
                            //            //    continue;
                            //            //}
                            //            SaveLogsToDb(log, tran, device);
                            //            SaveToParkingInDetails(log, tran, device);
                            //            if (CheckIfMemberCard(log.dwEnrollNumberInt, tran))
                            //            {
                            //                //To Deactivate member card
                            //                zkem.EnableUser(zkem.MachineNumber, log.dwEnrollNumberInt, zkem.MachineNumber, 10, false);
                            //            }
                            //        }
                            //        tran.Commit();
                            //    }
                            //}
                        }
                    }
                    else
                    {
                        device.Status = false;
                        device.GridBackground = new SolidColorBrush(Colors.Red);

                    }
                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    {
                        var tableName = device.DeviceType == 1 ? "deviceLog" : "exitdevicelog";
                        //var totalEnteredVechicle = conn.ExecuteScalar<int>($"SELECT count(*) FROM PARKINGINDETAILS p join devicelist d on p.vehicletype=d.vehicletype where deviceid='{device.DeviceId}' and convert(date, indate) = convert(date, getdate())");
                        //DeviceList.Where(x => x.VehicleType == device.VehicleType).ToList().ForEach(x => x.EnteredVehicle = totalEnteredVechicle);
                        device.EnteredVehicle = conn.ExecuteScalar<int>($"select Count(*) from {tableName} where DeviceId='{device.DeviceId}' and DATEFROMPARTS(dwYear, dwMonth, dwDay) = convert(date, getdate())");
                    }
                }
            }
            catch (Exception ex)
            {
                MainLogger.Error("From ExecuteReadLog: " + ex.Message.ToString());
            }
        }

        bool alreadyEntered = false;
        async Task SaveAccountBill()
        {
            if (!TimeToSaveAccount()) return;
            //bool parkingExists = await CheckIfParkingSalesAlreadyExist(GlobalClass.mcode);
            //if (parkingExists)
            //{
            //    MainLogger.Info("From SaveAccountBill: Parking sales Already exists");
            //}
            //else
            //{
            await SaveParkingAccount();
            //}

            //bool membershipExists = await CheckIfMembershipSalesAlreadyExist(GlobalClass.mcode);
            //if (membershipExists)
            //{
            //    MainLogger.Info("From SaveAccountBill: Membership sales Already exists");
            //}
            //else
            //{
            await SaveMembershipAccount();
            //}
        }

        private async Task SaveMembershipAccount()
        {
            var salesList = await GetMembershipSalesOfTheDay();
            if (salesList == null || salesList.Count() == 0)
            {
                MainLogger.Info("From SaveAccountBill: Membership Sales not found");
                return;
            }
            foreach (var sales in salesList)
            {
                var res = await ProductService.CheckIfMenuCodeExists(sales.prodid, sales.description);
                if (res == null)
                {
                    MainLogger.Error("From CheckIfMenuCodeExists: " + "End Point Not Found");
                    return;
                }
                string mcode;
                if (res.status == "ok")
                {
                    mcode = res.result.ToString();
                }
                else
                {
                    MainLogger.Error("From SaveMembershipAccount: " + res.result.ToString());
                    return;
                }
                BillMain billMain = new BillMain();
                billMain.division = "MMX";
                billMain.terminal = GlobalClass.Terminal;
                billMain.trnuser = GlobalClass.User.UserName;
                billMain.trnmode = "Cash";
                billMain.trnac = "AT01002";
                billMain.parac = "AT01002";
                billMain.guid = Guid.NewGuid().ToString();
                billMain.voucherAbbName = "TI";
                billMain.Orders = "";
                billMain.ConfirmedBy = GlobalClass.User.UserName;
                billMain.tender = sales.GrossAmount;


                //var TaxedAmount = item.NetAmount;

                Product product = new Product
                {
                    mcode = mcode,
                    quantity = 1,
                    rate = sales.Taxable,
                };
                billMain.prodList.Add(product);
                await SaveBillAndPrint(billMain);
            }

        }



        private async Task SaveParkingAccount()
        {
            var sales = await GetParkingSalesOfTheDay();
            if (sales == null || sales.GrossAmount == 0 || sales.Taxable == 0)
            {
                MainLogger.Info("From SaveAccountBill: Daily Sales not found");
                return;
            }

            BillMain billMain = new BillMain();
            billMain.division = "MMX";
            billMain.terminal = GlobalClass.Terminal;
            billMain.trnuser = GlobalClass.User.UserName;
            billMain.trnmode = "Cash";
            billMain.trnac = "AT01002";
            billMain.parac = "AT01002";
            billMain.guid = Guid.NewGuid().ToString();
            billMain.voucherAbbName = "TI";
            billMain.Orders = "";
            billMain.ConfirmedBy = GlobalClass.User.UserName;
            billMain.tender = sales.GrossAmount;


            //var TaxedAmount = item.NetAmount;

            Product product = new Product
            {
                mcode = GlobalClass.mcode,
                quantity = 1,
                rate = sales.Taxable,
            };
            billMain.prodList.Add(product);
            var res = await SaveBillAndPrint(billMain);
            if (res)
            {
                await SaveParkingAccount();
            }
        }

        private async Task<bool> CheckIfParkingSalesAlreadyExist(string mcode)
        {
            var res = await BillingService.CheckIfparkingSalesAlreadyExist(mcode);
            return res;
        }
        private async Task<bool> CheckIfMembershipSalesAlreadyExist(string mcode)
        {
            var res = await BillingService.CheckIfMembershipSalesAlreadyExist(mcode);
            return res;
        }

        private bool TimeToSaveAccount()
        {
            var TimeToSaveAccount = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, Convert.ToInt32(GlobalClass.clearhour), Convert.ToInt32(GlobalClass.clearminute), 0);
            //if (DateTime.Now >= TimeToSaveAccount && DateTime.Now <= TimeToSaveAccount.AddMinutes(1))
            if (DateTime.Now >= TimeToSaveAccount && DateTime.Now <= TimeToSaveAccount.AddMinutes(2))
            {
                if (!alreadyEntered)
                {
                    alreadyEntered = true;
                    return true;
                }
                return false;
            }
            alreadyEntered = false;
            return false;
        }

        private async Task<bool> SaveBillAndPrint(BillMain billMain)
        {
            var functionRes = await BillingService.SaveBill(billMain);
            if (functionRes.status == "1")
            {
                //PrintFunction.PrintBill(functionRes.result.ToString());
                //if (w != null)
                //{
                //    w.Close();
                //}
                //await SaveParkingAccount();
                return true;
            }
            else if (functionRes.status == "0")
            {
                MainLogger.Error(functionRes.result?.ToString());
                return false;
            }
            // to do log Couldn't save bill to database
            MainLogger.Error(functionRes.result?.ToString());
            return false;
        }
        private async Task<DailyTransactionDto> GetParkingSalesOfTheDay()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                conn.Open();
                var res = await BillingService.GetLastParkingBillDate(GlobalClass.mcode);
                if (res.status == "ok")
                {
                    var lastBillDate = Convert.ToDateTime(res.result).ToString("yyyy-MM-dd");

                    var sql1 = @"SELECT ISNULL(MIN(TDate), '2000-01-01') FROM ParkingSales  s join ParkingSalesDetails d on s.BillNo=d.BillNo where TDate>@TDate and PTYPE='p'";
                    var nextTransactionDate = conn.ExecuteScalar<DateTime>(sql1, new { TDate = lastBillDate });
                    var query = @"select SUM(s.GrossAmount) GrossAmount, SUM(s.Amount) Amount, SUM(s.VAT) vat,SUM(s.Taxable) Taxable,SUM(s.NonTaxable) nontaxable from parkingsales s join ParkingSalesDetails d on s.BillNo=d.BillNo  where TDate=@TDate and PTYPE='P'";
                    var result = conn.QueryFirstOrDefault<DailyTransactionDto>(query, new { TDate = nextTransactionDate });
                    return result;
                }
                MainLogger.Error(res.result.ToString());
                return null;
            }
        }
        private async Task<IEnumerable<dynamic>> GetMembershipSalesOfTheDay()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                conn.Open();
                var res = await BillingService.GetLastMembersBillDate(GlobalClass.mcode);

                if (res.status == "ok")
                {
                    var lastBillDate = Convert.ToDateTime(res.result).ToString("yyyy-MM-dd");

                    var query = @"select d.prodid,d.description, s.* from ParkingSales s join ParkingSalesDetails d on s.BillNo=d.BillNo where TDate>@TDate and PTYPE='c'";
                    var result = await conn.QueryAsync(query, new { TDate = lastBillDate });
                    return result;
                }
                MainLogger.Error(res.result.ToString());
                return null;
            }
        }
        string GetInterval(DateTime In, DateTime Out, string InTime, string OutTime)
        {
            var InDate = In.Add(DateTime.Parse(InTime).TimeOfDay);
            var OutDate = Out.Add(DateTime.Parse(OutTime).TimeOfDay);
            var interval = OutDate - InDate;
            return (interval.Days * 24 + interval.Hours).ToString() + " Hrs " + (interval.Minutes).ToString() + " Mins";
        }

        private void DisableExpiredMember(CZKEM zkem)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                conn.Open();
                var query = "select m.Barcode,* from members m join dailycards c on m.BARCODE=c.CardNumber where ExpiryDate < GETDATE()";

                //var sql = "SELECT * FROM Author A INNER JOIN Book B on A.AuthorId = B.Authorid";
                //var result = connection.Query<Book, Author, Book>(sql,
                //(b, a) => { b.Author = a; return b; }, splitOn: "AuthorId");


                var expiredMembers = conn.Query<Member, DailyCard, Member>(query, (m, c) => { m.DailyCard = c; return m; }, splitOn: "BARCODE");

                foreach (var member in expiredMembers)
                {
                    zkem.EnableUser(zkem.MachineNumber, member.DailyCard.CardId, zkem.MachineNumber, 10, false);
                    MainLogger.Info($"Disabled expired user: {member.Barcode}");
                }
            }
        }

        private DateTime GetDeviceDate(CZKEM zkem)
        {
            int Year = 0, Month = 0, Day = 0, Hour = 0, Minute = 0, Second = 0;
            zkem.GetDeviceTime(zkem.MachineNumber, ref Year, ref Month, ref Day, ref Hour, ref Minute, ref Second);
            var deviceDate = new DateTime(Year, Month, Day, Hour, Minute, Second);
            return deviceDate;
        }

        private void SyncDeviceDateAndClearDeviceData()
        {
            foreach (var device in DeviceList)
            {
                var zkem = new zkemkeeper.CZKEM();

                device.DeviceIp = device.DeviceIp.Trim();
                bool isValidIpA = UniversalStatic.ValidateIP(device.DeviceIp);
                if (!isValidIpA)
                {
                    MainLogger.Error($"From SyncDeviceDate: Invalid Ip: {device.DeviceIp}");
                    continue;
                }

                isValidIpA = UniversalStatic.PingTheDevice(device.DeviceIp);
                if (!isValidIpA)
                {
                    MainLogger.Error($"SyncDeviceDate: Failed to ping {device.DeviceIp}");
                    continue;
                }

                if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                {
                    int Year = 0, Month = 0, Day = 0, Hour = 0, Minute = 0, Second = 0;
                    zkem.GetDeviceTime(zkem.MachineNumber, ref Year, ref Month, ref Day, ref Hour, ref Minute, ref Second);
                    var deviceDate = new DateTime(Year, Month, Day, Hour, Minute, Second);
                    if (deviceDate != DateTime.Now)
                    {
                        zkem.SetDeviceTime(zkem.MachineNumber);
                        MainLogger.Info($"Date time synced with device: {device.DeviceIp}");
                    }


                    //if (device.DeviceType == (int)DeviceType.Entry)
                    //{
                    //    zkem.EnableDevice(zkem.MachineNumber, false);
                    //    MainLogger.Info($"Disabled device: {device.DeviceIp}");
                    //    MainLogger.Info($"Starting to ReadAllGLogData : {device.DeviceIp}");

                    //    if (zkem.ReadAllGLogData(zkem.MachineNumber))
                    //    {
                    //        if (zkem.GetGeneralLogData(zkem.MachineNumber, ref dwTMachineNumber, ref dwEnrollNumberInt, ref dwEMachineNumber, ref dwVerifyMode, ref dwInOutMode, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute))
                    //        {
                    //            while (zkem.GetGeneralLogData(zkem.MachineNumber, ref dwTMachineNumber, ref dwEnrollNumberInt, ref dwEMachineNumber, ref dwVerifyMode, ref dwInOutMode, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute))
                    //            {
                    //                if (dwEnrollNumberInt > short.MaxValue || dwEnrollNumberInt < short.MinValue)
                    //                {
                    //                    continue;
                    //                }
                    //                using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    //                {
                    //                    conn.Open();
                    //                    using (SqlTransaction tran = conn.BeginTransaction())
                    //                    {
                    //                        SaveLogsToDb(this, tran, device);
                    //                        SaveToParkingInDetails(this, tran, device);
                    //                        if (CheckIfMemberCard(this.dwEnrollNumberInt, tran))
                    //                        {
                    //                            //To Deactivate member card
                    //                            zkem.EnableUser(zkem.MachineNumber, this.dwEnrollNumberInt, zkem.MachineNumber, 10, false);
                    //                        }
                    //                        tran.Commit();
                    //                    }
                    //                }
                    //            }
                    //        }
                    //        else
                    //        {
                    //            MainLogger.Info($"No transaction data found on device: {device.DeviceIp}");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        MainLogger.Info($"No transaction data found on device: {device.DeviceIp}");
                    //    }
                    //    //clear data of current device
                    //    zkem.ClearGLog(zkem.MachineNumber);
                    //    MainLogger.Info($"Cleared logs of device: {device.DeviceIp}");

                    //    zkem.EnableDevice(zkem.MachineNumber, true);
                    //    MainLogger.Info($"Enabled device: {device.DeviceIp}");
                    //}
                }
            }

        }

        private bool SaveLogsToDb(MainViewModel log, SqlTransaction tran, Device device, int pid)
        {
            var alreadyExist = tran.Connection.QueryFirstOrDefault<DeviceLog>(@"Select * from deviceLog where dwTMachineNumber=@dwTMachineNumber and
                                                                                                                dwEMachineNumber=@dwEMachineNumber and
                                                                                                                dwEnrollNumberInt=@dwEnrollNumberInt and
                                                                                                                dwVerifyMode=@dwVerifyMode and
                                                                                                                dwInOutMode=@dwInOutMode and
                                                                                                                dwYear=@dwYear and
                                                                                                                dwMonth=@dwMonth and
                                                                                                                dwDay=@dwDay and
                                                                                                                dwHour=@dwHour and
                                                                                                                dwMinute=@dwMinute and
                                                                                                                DeviceIp=@DeviceIp and
                                                                                                                DeviceId=@DeviceId and
                                                                                                                pid=@pid", new
            {
                dwTMachineNumber = log.dwTMachineNumber,
                dwEMachineNumber = log.dwEMachineNumber,
                dwEnrollNumberInt = log.dwEnrollNumberInt,
                dwVerifyMode = log.dwVerifyMode,
                dwInOutMode = log.dwInOutMode,
                dwYear = log.dwYear,
                dwMonth = log.dwMonth,
                dwDay = log.dwDay,
                dwHour = log.dwHour,
                dwMinute = log.dwMinute,
                DeviceIp = device.DeviceIp,
                DeviceId = device.DeviceId,
                pid = pid
            }, tran);
            if (alreadyExist == null)
            {
                string strSQL = @"INSERT INTO deviceLog(
                                                        id,                   
                                                        dwTMachineNumber,
                                                        dwEMachineNumber,
                                                        dwEnrollNumberInt,
                                                        dwVerifyMode,
                                                        dwInOutMode,
                                                        dwYear,
                                                        dwMonth,
                                                        dwDay,
                                                        dwHour,
                                                        dwMinute,
                                                        DeviceIp,
                                                        DeviceId,
                                                        pid) 
                                                    values(
                                                        (SELECT ISNULL(MAX(ID),0) +1 FROM deviceLog),
                                                        @dwTMachineNumber,
                                                        @dwEMachineNumber,
                                                        @dwEnrollNumberInt,
                                                        @dwVerifyMode,
                                                        @dwInOutMode,
                                                        @dwYear,
                                                        @dwMonth,
                                                        @dwDay,
                                                        @dwHour,
                                                        @dwMinute,
                                                        @DeviceIp,
                                                        @DeviceId,
                                                        @pid)";
                tran.Connection.Execute(strSQL, new
                {
                    dwTMachineNumber = log.dwTMachineNumber,
                    dwEMachineNumber = log.dwEMachineNumber,
                    dwEnrollNumberInt = log.dwEnrollNumberInt,
                    dwVerifyMode = log.dwVerifyMode,
                    dwInOutMode = log.dwInOutMode,
                    dwYear = log.dwYear,
                    dwMonth = log.dwMonth,
                    dwDay = log.dwDay,
                    dwHour = log.dwHour,
                    dwMinute = log.dwMinute,
                    DeviceIp = device.DeviceIp,
                    DeviceId = device.DeviceId,
                    pid = pid
                }, tran);
                MainLogger.Info($"Saved logs of device: {device.DeviceIp}");
                return true;
            }
            return false;
        }
        private void SaveLogsToExitDb(MainViewModel log, SqlTransaction tran, Device device, int pid)
        {
            var alreadyExist = tran.Connection.QueryFirstOrDefault<DeviceLog>(@"Select * from exitdevicelog where dwTMachineNumber=@dwTMachineNumber and
                                                                                                                dwEMachineNumber=@dwEMachineNumber and
                                                                                                                dwEnrollNumberInt=@dwEnrollNumberInt and
                                                                                                                dwVerifyMode=@dwVerifyMode and
                                                                                                                dwInOutMode=@dwInOutMode and
                                                                                                                dwYear=@dwYear and
                                                                                                                dwMonth=@dwMonth and
                                                                                                                dwDay=@dwDay and
                                                                                                                dwHour=@dwHour and
                                                                                                                dwMinute=@dwMinute and
                                                                                                                DeviceIp=@DeviceIp and
                                                                                                                DeviceId=@DeviceId and
                                                                                                                pid=@pid", new
            {
                dwTMachineNumber = log.dwTMachineNumber,
                dwEMachineNumber = log.dwEMachineNumber,
                dwEnrollNumberInt = log.dwEnrollNumberInt,
                dwVerifyMode = log.dwVerifyMode,
                dwInOutMode = log.dwInOutMode,
                dwYear = log.dwYear,
                dwMonth = log.dwMonth,
                dwDay = log.dwDay,
                dwHour = log.dwHour,
                dwMinute = log.dwMinute,
                DeviceIp = device.DeviceIp,
                DeviceId = device.DeviceId,
                pid = pid
            }, tran);
            if (alreadyExist == null)
            {
                string strSQL = @"INSERT INTO exitdevicelog(
                                                        id,                   
                                                        dwTMachineNumber,
                                                        dwEMachineNumber,
                                                        dwEnrollNumberInt,
                                                        dwVerifyMode,
                                                        dwInOutMode,
                                                        dwYear,
                                                        dwMonth,
                                                        dwDay,
                                                        dwHour,
                                                        dwMinute,
                                                        DeviceIp,
                                                        DeviceId,
                                                        pid
                                                        ) 
                                                    values(
                                                        (SELECT ISNULL(MAX(ID),0) +1 FROM exitdevicelog),
                                                        @dwTMachineNumber,
                                                        @dwEMachineNumber,
                                                        @dwEnrollNumberInt,
                                                        @dwVerifyMode,
                                                        @dwInOutMode,
                                                        @dwYear,
                                                        @dwMonth,
                                                        @dwDay,
                                                        @dwHour,
                                                        @dwMinute,
                                                        @DeviceIp,
                                                        @DeviceId,
                                                        @pid                        
                                                        )";
                tran.Connection.Execute(strSQL, new
                {
                    dwTMachineNumber = log.dwTMachineNumber,
                    dwEMachineNumber = log.dwEMachineNumber,
                    dwEnrollNumberInt = log.dwEnrollNumberInt,
                    dwVerifyMode = log.dwVerifyMode,
                    dwInOutMode = log.dwInOutMode,
                    dwYear = log.dwYear,
                    dwMonth = log.dwMonth,
                    dwDay = log.dwDay,
                    dwHour = log.dwHour,
                    dwMinute = log.dwMinute,
                    DeviceIp = device.DeviceIp,
                    DeviceId = device.DeviceId,
                    pid = pid
                }, tran);
                MainLogger.Info($"Saved logs of exit device: {device.DeviceIp}");
            }
        }


        private bool CheckIfMemberCard(int cardId, SqlTransaction tran)
        {
            var isMember = tran.Connection.QueryFirstOrDefault<ParkingIn>(@"select * from DailyCards c join Members m on c.CardNumber=m.BARCODE where cardid=@cardId
", new { cardId }, tran);
            if (isMember == null)
            {
                return false;
            }
            return true;
        }

        private void SaveToParkingInDetails(MainViewModel ld, SqlTransaction tran, Device device)
        {
            try
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
                    Parking.InTime = logDate.ToString("hh:mm:ss tt");
                    var nepDate = new DateConverter(GlobalClass.TConnectionString);
                    Parking.InMiti = nepDate.CBSDate(Parking.InDate);
                    Parking.VehicleType = GetVechicleTypeByDeviceId(device.DeviceId);

                    var alreadyExist = tran.Connection.QueryFirstOrDefault<ParkingIn>($"Select * from parkingindetails where fyid=@fyid and vehicletype=@vehicletype and indate=@indate and inmiti=@inmiti and intime=@intime and barcode=@Barcode", Parking, tran);
                    //var alreadyExist = tran.Connection.QueryFirstOrDefault<ParkingIn>($"Select * from parkingindetails where ParkingInDetails.PID not in (Select PID from ParkingOutDetails where FYID=@fyid) and barcode=@Barcode", Parking, tran);
                    if (alreadyExist == null)
                    {
                        string strSQL = @"INSERT INTO ParkingInDetails(PID, FYID, VehicleType, InDate, InTime, PlateNo, Barcode, [UID], InMiti, SESSION_ID)
                          Values(@PID, @FYID, @VehicleType,@InDate,@InTime,@PlateNo,@Barcode,@UID,@InMiti, @SESSION_ID)";
                        tran.Connection.Execute(strSQL, Parking, tran);

                        tran.Connection.Execute("UPDATE tblSequence SET CurNo = CurNo + 1 WHERE VNAME = 'PID' AND FYID = " + GlobalClass.FYID, transaction: tran);
                        SaveLogsToDb(ld, tran, device, Parking.PID);
                    }
                    //else
                    //{
                    //    MainLogger.Info("---------------From SaveLogsToDb: Already Exists:---------------");
                    //    string jsonLogsObj = JsonConvert.SerializeObject(alreadyExist);
                    //    MainLogger.Info(jsonLogsObj);
                    //    MainLogger.Info("---------------End SaveLogsToDb: Already Exists:----------------");

                    //}

                }
                else
                {
                    //MessageBox.Show($"CardId: {ld.dwEnrollNumberInt} is not present in database!");
                    MainLogger.Error($"CardId: {ld.dwEnrollNumberInt} is not present in database!");
                }
            }
            catch (Exception ex)
            {
                MainLogger.Error("From SaveLogsToDb: " + ex.Message.ToString());
            }
        }
        private bool SaveToParkingOutDetails(MainViewModel ld, SqlTransaction tran, Device device)
        {

            string sql = $@"Select p.* from devicelog l join DeviceList d on l.DeviceId=d.DeviceId join ParkingInDetails p on p.PID=l.pid where IsMemberDevice=1 and DeviceType=1 and p.pid not in(select pid from ParkingOutDetails where FYID=@FYID) and dwEnrollNumberInt=@dwEnrollNumberInt
";
            var PIN = tran.Connection.QueryFirstOrDefault<ParkingIn>(sql, new { ld.dwEnrollNumberInt, fyid = GlobalClass.FYID }, tran);
            if (PIN == null)
            {
                //MessageBox.Show("Invalid barcode readings.", MessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                //PIN.Barcode = string.Empty;
                return false;
            }
            ParkingOut POUT = new ParkingOut();
            //var nepDate = new DateConverter(GlobalClass.TConnectionString);

            //DateTime ServerTime = nepDate.GetServerTime();
            var logDate = new DateTime(ld.dwYear, ld.dwMonth, ld.dwDay, ld.dwHour, ld.dwMinute, ld.dwSecond);
            //POUT.InDate = new DateTime(ld.dwYear, ld.dwMonth, ld.dwDay);
            //POUT.InTime = logDate.ToString("hh:mm:ss tt");
            //var nepDate = new DateConverter(GlobalClass.TConnectionString);
            //POUT.InMiti = nepDate.CBSDate(Parking.InDate);


            POUT.OutDate = new DateTime(ld.dwYear, ld.dwMonth, ld.dwDay);
            POUT.OutTime = logDate.ToString("hh:mm:ss tt");
            var nepDate = new DateConverter(GlobalClass.TConnectionString);
            POUT.OutMiti = nepDate.CBSDate(POUT.OutDate);

            POUT.Interval = GetInterval(PIN.InDate, POUT.OutDate, PIN.InTime, POUT.OutTime);
            POUT.PID = PIN.PID;
            POUT.Rate_ID = (int)tran.Connection.ExecuteScalar("SELECT RATE_ID FROM RATEMASTER WHERE IsDefault = 1", transaction: tran);
            POUT.FYID = GlobalClass.FYID;
            POUT.SESSION_ID = 1;

            var sql2 = "Select * from parkingoutdetails where PID=@PID and FYID=@FYID and OutDate=@OutDate and OutMiti=@OutMiti and OutTime=@OutTime and Interval=@Interval and Rate_ID=@Rate_ID and ChargedAmount=@ChargedAmount and CashAmount=@CashAmount and RoyaltyAmount=@RoyaltyAmount and Remarks=@Remarks and UID=@UID and ChargedHours=@ChargedHours and SESSION_ID=@SESSION_ID and STAFF_BARCODE=@STAFF_BARCODE and BILLTO=@BILLTO and BILLTOADD=@BILLTOADD and BILLTOPAN=@BILLTOPAN";
            var alreadyExist = tran.Connection.QueryFirstOrDefault(sql2, POUT, tran);
            if (alreadyExist == null)
            {
                POUT.Save(tran);
                SaveLogsToExitDb(ld, tran, device, POUT.PID);

                return true;
            }
            return false;
        }

        private void DeActivateEnteredCard(int dwEnrollNumberInt)
        {
            //zkem.EnableUser(zkem.MachineNumber, cardId, zkem.MachineNumber, 10, true);
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
