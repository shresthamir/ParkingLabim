using Dapper;
using ExcelDataReader;
using Microsoft.Win32;
using ParkingManagement.Enums;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ParkingManagement.ViewModel
{
    public class RegisterDailyCardViewModel : BaseViewModel
    {
        private ObservableCollection<Device> _Device;
        private ObservableCollection<DailyCard> _DailyCard;
        private string ipaddress;

        //public CollectionViewSource ViewSource { get { return _ViewSource; } set { _ViewSource = value; OnPropertyChanged("ViewSource"); } }


        public ObservableCollection<Device> DeviceList { get { return _Device; } set { _Device = value; OnPropertyChanged("DeviceList"); } }
        public ObservableCollection<DailyCard> DailyCardList { get { return _DailyCard; } set { _DailyCard = value; OnPropertyChanged("DailyCardList"); } }
        public RelayCommand BrowseCommand { get; set; }
        public RelayCommand UploadCommand { get; set; }

        public RegisterDailyCardViewModel()
        {
            DailyCardList = new ObservableCollection<DailyCard>();
            GetDeviceList();
            BrowseCommand = new RelayCommand(ExecuteBrowse);
            UploadCommand = new RelayCommand(ExecuteUpload);
            //var zkem = new zkemkeeper.CZKEM();
            //zkem.ClearAdministrators(zkem.MachineNumber);
            //if (zkem.Connect_Net("192.168.125.233", 4370))
            //{
            //    zkem.GetDeviceIP(zkem.MachineNumber, ref ipaddress);

            //    zkem.SetDeviceIP(zkem.MachineNumber, "192.168.125.234");
            //    zkem.GetDeviceIP(zkem.MachineNumber, ref ipaddress);
            //}
            //else
            //{

            //}


        }

        private async void ExecuteUpload(object obj)
        {
            try
            {
                foreach (var device in DeviceList.Where(x=>x.DeviceType == (int)DeviceType.Entry))
                {
                    int maxCardId = await GetCardIdFromDb();

                    var zkem = new zkemkeeper.CZKEM();

                    device.DeviceIp = device.DeviceIp.Trim();
                    bool isValidIpA = UniversalStatic.ValidateIP(device.DeviceIp);
                    if (!isValidIpA)
                    {
                        MessageBox.Show($"The Device with IP: {device.DeviceIp} has invalid IP!!");
                        continue;
                    }

                    isValidIpA = UniversalStatic.PingTheDevice(device.DeviceIp);
                    if (!isValidIpA)
                    {
                        MessageBox.Show($"The device at " + device.DeviceIp + ":" + device.DevicePort + " did not respond!!");
                        continue;
                    }

                    if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                    {

                        //zkem.GetDeviceIP(zkem.MachineNumber, ref ipaddress);

                        //zkem.SetDeviceIP(zkem.MachineNumber, "192.168.125.234");
                        //zkem.GetDeviceIP(zkem.MachineNumber, ref ipaddress);



                        foreach (var card in DailyCardList)
                        {
                            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                            {
                                var res = await conn.QueryAsync<DailyCard>("select * from dailycards where cardnumber=@cardnumber", new { card.CardNumber });
                                if (res?.Count() == 0)
                                {
                                    card.CardId = maxCardId;
                                    zkem.SetStrCardNumber(card.CardNumber);
                                    zkem.SetUserInfo(zkem.MachineNumber, maxCardId, card.CardNumber, "", 0, true);
                                    maxCardId++;


                                    card.DeviceList.Where(x => x.DeviceId == device.DeviceId).ToList().ForEach(x => x.Status = true);

                                    //if (device.DeviceId == 1)
                                    //{
                                    //    card.Device1 = 1;
                                    //}
                                    //else if (device.DeviceId == 2)
                                    //{
                                    //    card.Device2 = 1;
                                    //}
                                    //else if (device.DeviceId == 3)
                                    //{
                                    //    card.Device3 = 1;
                                    //}
                                }
                                else
                                {
                                    card.CardId = res.FirstOrDefault().CardId;
                                    zkem.SetStrCardNumber(card.CardNumber);
                                    zkem.SetUserInfo(zkem.MachineNumber, card.CardId, card.CardNumber, "", 0, true);

                                    card.DeviceList.Where(x => x.DeviceId == device.DeviceId).ToList().ForEach(x => x.Status = true);

                                    //if (device.DeviceId == 1)
                                    //{
                                    //    card.Device1 = 1;
                                    //}
                                    //else if (device.DeviceId == 2)
                                    //{
                                    //    card.Device2 = 1;
                                    //}
                                    //else if (device.DeviceId == 3)
                                    //{
                                    //    card.Device3 = 1;
                                    //}
                                }
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Connecting to device {device.Devicename} failed!", "Daily Card Registraion", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                SaveToDb(DailyCardList);
                MessageBox.Show("Operation completed!", "Daily Card Registraion", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Daily Card Registraion", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }



        private async Task<int> GetCardIdFromDb()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                var maxCardId = await conn.QueryAsync<int>("SELECT CASE WHEN MAX(cardId) IS NULL THEN 1 ELSE MAX(cardId) + 1 END  FROM DailyCards");
                return maxCardId.FirstOrDefault();
            }
        }

        private async void SaveToDb(ObservableCollection<DailyCard> dailyCardList)
        {
            try
            {
                foreach (var item in dailyCardList)
                {
                    using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
                    {
                        var card = await conn.QueryAsync<DailyCard>("select * from dailycards where cardnumber=@cardnumber", new { item.CardNumber });
                        if (card?.Count() == 0)
                        {
                            //newCardList.Add(item);
                            await conn.ExecuteAsync("insert into DailyCards(cardId,CardNumber) values(@cardId,@cardNumber)", item);
                        }
                    }
                }

            }
            catch (Exception)
            {
                //throw;
            }
        }

        private async void ExecuteBrowse(object obj)
        {
            var tbContainer = getExcelDataToDataTable();

            foreach (var item in tbContainer.AsEnumerable().ToList())
            {
                string cardNumber = item.ItemArray.FirstOrDefault().ToString();
                if (!string.IsNullOrEmpty(cardNumber))
                {
                    if (!DailyCardList.Any(x => x.CardNumber == cardNumber))
                    {
                        DailyCardList.Add(new DailyCard { CardNumber = cardNumber, DeviceList = DeviceList });
                    }
                }
            }

        }

        private void GetDeviceList()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                DeviceList = new ObservableCollection<Device>(conn.Query<Device>("SELECT * FROM DeviceList WHERE IsMemberDevice = 0"));
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
        public static DataTable getExcelDataToDataTable()
        {
            try
            {
                DataTable dt = new DataTable();
                OpenFileDialog openFile = new OpenFileDialog();
                openFile.DefaultExt = ".xlsx";
                openFile.Filter = "(.xlsx)|*.xlsx";

                var browsefile = openFile.ShowDialog();
                if (browsefile == true)
                {
                    using (var stream = System.IO.File.Open(openFile.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // Auto-detect format, supports:
                        //  - Binary Excel files (2.0-2003 format; *.xls)
                        //  - OpenXml Excel files (2007 format; *.xlsx)                        
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                                {
                                    UseHeaderRow = true
                                }
                            });
                            dt = result.Tables[0];
                        }
                    }
                }
                return dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.GetBaseException().Message, "Sync", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}
