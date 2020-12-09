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
using System.Runtime.InteropServices;

namespace AccessControlDownloader.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string ConnectionString;
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

        public ObservableCollection<Device> DeviceList { get { return _Device; } set { _Device = value; OnPropertyChanged("DeviceList"); } }
        public DateTime DateToday { get { return _DateToday; } set { _DateToday = value; OnPropertyChanged("DateToday"); } }
        public string Message { get { return _Message; } set { _Message = value; OnPropertyChanged("Message"); } }
        public MainViewModel()
        {
            SqlConnectionStringBuilder sbr = new SqlConnectionStringBuilder(GlobalClass.DataConnectionString);
            sbr.ApplicationName = "AccessControlDownloader";
            ConnectionString = sbr.ConnectionString;
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
        DispatcherTimer dispatchTimer;
        DispatcherTimer ContingencyTimer;
        public void StartTimer()
        {
            DateToday = DateTime.Now;
            ContingencyTimer = new DispatcherTimer();
            ContingencyTimer.Tick += new EventHandler(ContingencyTimer_Tick);
            ContingencyTimer.Interval = new TimeSpan(0, 1, 0);
            ContingencyTimer.Start();

            dispatchTimer = new DispatcherTimer();
            dispatchTimer.Tick += new EventHandler(DispatchTime_Tick);
            dispatchTimer.Interval = new TimeSpan(0, 1, 0);
            dispatchTimer.Start();
            Task.Run(async () =>
            {
                await ExecuteReadLog(true);
            });
        }

        private void ContingencyTimer_Tick(object sender, EventArgs e)
        {
            if (DateToday.AddMinutes(5) < DateTime.Now)
            {
                if (!dispatchTimer.IsEnabled)
                    dispatchTimer.Start();
                else
                    Message = "The Sync App is not working properly. Please restart the app.";
            }
        }

        private int timerToSyncDeviceTime = 0;
        private void DispatchTime_Tick(object sender, EventArgs e)
        {
            dispatchTimer.Stop();
            Task.Run(async () =>
            {
                try
                {
                    await ExecuteReadLog(false);
                    //await SaveAccountBill();
                    timerToSyncDeviceTime++;
                    DateToday = DateTime.Now;
                    dispatchTimer.Start();
                }
                catch (Exception ex)
                {
                    if (!dispatchTimer.IsEnabled)
                        dispatchTimer.Start();
                    MainLogger.Error("From DispatchTime_Tick: " + ex.GetBaseException().Message);
                }
            });
        }

        private void GetDeviceList()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                DeviceList = new ObservableCollection<Device>(conn.Query<Device>("SELECT D.DeviceId, D.DeviceName, D.DeviceIP, D.DevicePort, D.VehicleType, D.IsMemberDevice, D.DeviceType, V.VTypeID, V.Description, V.Capacity, V.UID, V.ButtonImage FROM DeviceList D JOIN VehicleType V on D.VehicleType = V.VTypeID"));
                foreach (Device vtype in DeviceList)
                {
                    if ((vtype.ButtonImage?.Length ?? 0) > 0)
                        vtype.ImageSource = Imaging.BinaryToImage(vtype.ButtonImage);
                }
            }
        }
        private void GetAdminUser()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
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

        private async Task ExecuteReadLog(bool initialLoad = false)
        {
            try
            {
                await Task.Yield();
                // Skip logs of exit device 
                //foreach (var device in DeviceList.Where(x => x.DeviceType == (int)DeviceType.Entry))
                foreach (Device device in DeviceList)
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
                    }

                    MainLogger.Info($"{device.DeviceIp} is valid IP Address");

                    isValidIpA = UniversalStatic.PingTheDevice(device.DeviceIp);
                    if (!isValidIpA)
                    {
                        device.Status = false;
                        device.GridBackground = new SolidColorBrush(Colors.Red);
                        MainLogger.Info($"Failed to ping {device.DeviceIp}");
                        continue;
                    }
                    MainLogger.Info($"Ping successfull to {device.DeviceIp}");

                    if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                    {
                        MainLogger.Info($"Connected to Device sucessfully: {device.DeviceIp}");
                        device.Status = true;
                        device.GridBackground = new SolidColorBrush(Colors.LightGreen);

                        ////To get device time every hour
                        if (initialLoad || timerToSyncDeviceTime == 60)
                        {
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
                            int counter = 0;
                            //MainViewModel ld = new MainViewModel();
                            while (zkem.GetGeneralLogData(zkem.MachineNumber, ref dwTMachineNumber, ref dwEnrollNumberInt, ref dwEMachineNumber, ref dwVerifyMode, ref dwInOutMode, ref dwYear, ref dwMonth, ref dwDay, ref dwHour, ref dwMinute))
                            {
                                MainLogger.Debug($"{DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt")} {dwEnrollNumberInt.ToString().PadRight(15, ' ')} {dwYear} {dwMonth} {dwDay} {dwHour} {dwMinute}");
                                if (dwEnrollNumberInt > short.MaxValue || dwEnrollNumberInt < short.MinValue)
                                {
                                    continue;
                                }
                                counter++;
                                if (device.DeviceType == (int)DeviceType.Entry)
                                {
                                    SaveLogsToDb(this, device);

                                    await SaveToParkingInDetails(this, device);


                                    //if (device.IsMemberDevice == 1)
                                    //{
                                    //    zkem.EnableUser(zkem.MachineNumber, dwEnrollNumberInt, zkem.MachineNumber, 10, false);
                                    //}
                                    ////Commented By Amir [Remove after review]
                                    ////string query = "select CardId from parkingindetails p join DailyCards d on p.Barcode=d.CardNumber where pid not in(select pid from ParkingOutDetails)";
                                    ////var res = conn.Query<int>(query, transaction: tran);
                                    ////if (res != null)
                                    ////{
                                    ////    foreach (var id in res)
                                    ////    {
                                    ////        //if (CheckIfMemberCard(this.dwEnrollNumberInt, tran))
                                    ////        if (CheckIfMemberCard(id, tran))
                                    ////        {
                                    ////            //To Deactivate member card
                                    ////            zkem.EnableUser(zkem.MachineNumber, id, zkem.MachineNumber, 10, false);
                                    ////            MainLogger.Info($"Card with id: {id} is disabled.");
                                    ////        }
                                    ////    }
                                    ////}

                                }
                                else if (device.DeviceType == (int)DeviceType.Exit)
                                {
                                    using (SqlConnection conn = new SqlConnection(ConnectionString))
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

                            //clear data of current device
                            //if (timerToSyncDeviceTime == 60)
                            //{
                            zkem.EnableDevice(zkem.MachineNumber, false);
                            MainLogger.Info($"Disabled device to clear logs: {device.DeviceIp}");

                            zkem.ClearGLog(zkem.MachineNumber);
                            MainLogger.Info($"Cleared logs of device: {device.DeviceIp}");

                            zkem.EnableDevice(zkem.MachineNumber, true);
                            MainLogger.Info($"Enabled device after clrearing logs: {device.DeviceIp}");

                            DisableExpiredMember(zkem);
                            if (counter > 0)
                            {
                                device.LastDownloadTime = DateTime.Now.ToString("hh:mm tt");
                                device.LastDownloadCount = counter;
                            }
                            //}
                        }
                        zkem.Disconnect();
                        Marshal.ReleaseComObject(zkem);
                    }
                    else
                    {
                        device.Status = false;
                        device.GridBackground = new SolidColorBrush(Colors.Red);
                    }
                    using (SqlConnection conn = new SqlConnection(ConnectionString))
                    {
                        var tableName = device.DeviceType == 1 ? "deviceLog" : "exitdevicelog";
                        //var totalEnteredVechicle = conn.ExecuteScalar<int>($"SELECT count(*) FROM PARKINGINDETAILS p join devicelist d on p.vehicletype=d.vehicletype where deviceid='{device.DeviceId}' and convert(date, indate) = convert(date, getdate())");
                        //DeviceList.Where(x => x.VehicleType == device.VehicleType).ToList().ForEach(x => x.EnteredVehicle = totalEnteredVechicle);
                        device.EnteredVehicle = conn.ExecuteScalar<int>($"select Count(*) from {tableName} where DeviceId='{device.DeviceId}' and dwYear + '-' + dwMonth + '-' + dwDay = convert(date, getdate())");
                    }
                }
            }
            catch (Exception ex)
            {
                MainLogger.Error("From ExecuteReadLog: " + ex.GetBaseException().ToString());
            }
        }

        bool alreadyEntered = false;
        private string _Message;

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
                billMain.trnmode = sales.TRNMODE == true ? "Cash" : "Credit";
                //billMain.trnac = "AT01002";
                billMain.trnac = billMain.trnmode == "Cash" ? "AT01002" : await CheckIfAccodeExists(sales);
                billMain.parac = billMain.trnac;
                billMain.guid = Guid.NewGuid().ToString();
                billMain.voucherAbbName = "TI";
                billMain.Orders = "";
                billMain.ConfirmedBy = GlobalClass.User.UserName;
                billMain.tender = sales.GrossAmount;
                billMain.TRNDATE = sales.TDate;
                billMain.TRN_DATE = sales.TDate;
                billMain.TRNTIME = sales.TRNTIME;
                billMain.billto = sales.BillTo;
                billMain.billtoadd = sales.BILLTOADD;

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

        private async Task<string> CheckIfAccodeExists(dynamic sales)
        {
            var res = await BillingService.CheckIfAccCodeAlreadyExist(sales);
            return res;
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
            billMain.TRNDATE = sales.TDate;
            billMain.TRN_DATE = sales.TDate;
            billMain.TRNTIME = DateTime.Now.ToString("hh:mm:ss tt");


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
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                var res = await BillingService.GetLastParkingBillDate(GlobalClass.mcode);
                if (res.status == "ok")
                {
                    var lastBillDate = Convert.ToDateTime(res.result).ToString("yyyy-MM-dd");

                    var sql1 = @"SELECT ISNULL(MIN(TDate), '2000-01-01') FROM ParkingSales  s join ParkingSalesDetails d on s.BillNo=d.BillNo where TDate>@TDate and PTYPE='p'";
                    var nextTransactionDate = conn.ExecuteScalar<DateTime>(sql1, new { TDate = lastBillDate });
                    //var query = @"select SUM(s.GrossAmount) GrossAmount, SUM(s.Amount) Amount, SUM(s.VAT) vat,SUM(s.Taxable) Taxable,SUM(s.NonTaxable) nontaxable from parkingsales s join ParkingSalesDetails d on s.BillNo=d.BillNo  where TDate=@TDate and PTYPE='P'";
                    var query = @"select SUM(s.GrossAmount) GrossAmount, SUM(s.Amount) Amount, SUM(s.VAT) vat,SUM(s.Taxable) Taxable,SUM(s.NonTaxable) nontaxable,TDate from parkingsales s join ParkingSalesDetails d on s.BillNo=d.BillNo  where TDate=@TDate and PTYPE='P' group by TDate";
                    var result = conn.QueryFirstOrDefault<DailyTransactionDto>(query, new { TDate = nextTransactionDate });
                    return result;
                }
                MainLogger.Error(res.result.ToString());
                return null;
            }
        }
        private async Task<IEnumerable<dynamic>> GetMembershipSalesOfTheDay()
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
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
            using (SqlConnection conn = new SqlConnection(ConnectionString))
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
                }
            }
        }

        private bool SaveLogsToDb(MainViewModel log, Device device, int pid = 0)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
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
                conn.Execute(strSQL, new
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
                });
            }
            MainLogger.Info($"Saved logs of device: {device.DeviceIp}");
            return true;
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

        private async Task SaveToParkingInDetails(MainViewModel ld, Device device, bool Retry = false)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        ParkingIn Parking = new ParkingIn();
                        Parking.Barcode = await GetCardNumberByEnrollId(ld.dwEnrollNumberInt);
                        if (string.IsNullOrEmpty(Parking.Barcode))
                        {
                            MainLogger.Error($"CardId: {ld.dwEnrollNumberInt} is not present in database!");
                            return;
                        }
                        Parking.FYID = GlobalClass.FYID;
                        Parking.UID = GlobalClass.User.UID;
                        //Parking.PlateNo = ld.dwEnrollNumber;
                        Parking.SESSION_ID = 1;
                        var logDate = new DateTime(ld.dwYear, ld.dwMonth, ld.dwDay, ld.dwHour, ld.dwMinute, ld.dwSecond);
                        Parking.InDate = new DateTime(ld.dwYear, ld.dwMonth, ld.dwDay);
                        Parking.InTime = logDate.ToString("hh:mm:ss tt");
                        Parking.InMiti = tran.Connection.ExecuteScalar<string>("select MITI from datemiti WHERE AD = @InDate", new { Parking.InDate }, tran);
                        Parking.VehicleType = device.VehicleType;

                        var alreadyExist = tran.Connection.QueryFirstOrDefault<ParkingIn>($"Select * from parkingindetails where fyid = @fyid and vehicletype=@vehicletype and indate=@indate and inmiti=@inmiti and intime=@intime and barcode=@Barcode", Parking, tran);
                        //var alreadyExist = tran.Connection.QueryFirstOrDefault<ParkingIn>($"Select * from parkingindetails where ParkingInDetails.PID not in (Select PID from ParkingOutDetails where FYID=@fyid) and barcode=@Barcode", Parking, tran);
                        if (alreadyExist == null)
                        {
                            Parking.PID = Convert.ToInt32(await GetPID("PID", tran));
                            string strSQL = @"INSERT INTO ParkingInDetails(PID, FYID, VehicleType, InDate, InTime, PlateNo, Barcode, [UID], InMiti, SESSION_ID)
                          Values(@PID, @FYID, @VehicleType,@InDate,@InTime,@PlateNo,@Barcode,@UID,@InMiti, @SESSION_ID)";
                            tran.Connection.Execute(strSQL, Parking, tran);
                            tran.Commit();
                        }
                    }
                }
            }
            catch (SqlException SqlEx)
            {
                if (SqlEx.Number == 2627)
                {
                    await GetPID("PID");
                    if (!Retry)
                        await SaveToParkingInDetails(ld, device, true);
                }
                MainLogger.Error("From SaveLogsToDb: " + SqlEx.GetBaseException().Message);
                MainLogger.Error(Newtonsoft.Json.JsonConvert.SerializeObject(new { ld.dwEnrollNumberInt, ld.dwYear, ld.dwMonth, ld.dwDay, ld.dwHour, ld.dwMinute, ld.dwSecond, device.DeviceIp }, Formatting.Indented));
            }
            catch (Exception ex)
            {
                MainLogger.Error("From SaveLogsToDb: " + ex.GetBaseException().Message);
                MainLogger.Error(Newtonsoft.Json.JsonConvert.SerializeObject(new { ld.dwEnrollNumberInt, ld.dwYear, ld.dwMonth, ld.dwDay, ld.dwHour, ld.dwMinute, ld.dwSecond, device.DeviceIp }, Formatting.Indented));
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
            POUT.OutMiti = tran.Connection.ExecuteScalar<string>("select MITI from datemiti WHERE AD = @OutDate", new { POUT.OutDate }, tran);

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
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                int cardid = conn.QueryFirstOrDefault<int>($"select max(isnull(cardid,0)) from dailycards");
                return cardid;
            }
        }



        private async Task<string> GetCardNumberByEnrollId(int dwEnrollNumberInt)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string cardNumber = await conn.QueryFirstOrDefaultAsync<string>($"select cardnumber from dailycards where cardid = @dwEnrollNumberInt", new { dwEnrollNumberInt });
                return cardNumber;
            }
        }


        async Task<string> GetPID(string VNAME)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        string pid = await GetPID(VNAME, tran);
                        tran.Commit();
                        return pid;
                    }
                }
            }
            catch (Exception ex)
            {
                MainLogger.Error("From GetPID: " + ex.GetBaseException().Message);
                return string.Empty;
            }
        }
        async Task<string> GetPID(string VNAME, SqlTransaction tran)
        {
            string invoice;
            invoice = await tran.Connection.ExecuteScalarAsync<string>("UPDATE tblSequence SET CurNo = CurNo + 1 OUTPUT DELETED.CurNo WHERE VNAME = @VNAME AND FYID = @FYID", new { VNAME = VNAME, FYID = GlobalClass.FYID }, tran);
            if (string.IsNullOrEmpty(invoice))
            {
                await tran.Connection.ExecuteAsync("INSERT INTO tblSequence(VNAME, FYID, CurNo) VALUES(@VNAME, @FYID, 1)", new { VNAME = VNAME, FYID = GlobalClass.FYID }, tran);
                invoice = "1";
            }
            return invoice;
        }
    }
}
