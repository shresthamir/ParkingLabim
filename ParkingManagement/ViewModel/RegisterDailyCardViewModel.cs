using Dapper;
using ExcelDataReader;
using Microsoft.Win32;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ParkingManagement.ViewModel
{
    public class RegisterDailyCardViewModel : BaseViewModel
    {
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
        private ObservableCollection<DailyCard> _DailyCard;
        private CollectionViewSource _ViewSource;

        //public CollectionViewSource ViewSource { get { return _ViewSource; } set { _ViewSource = value; OnPropertyChanged("ViewSource"); } }


        public ObservableCollection<Device> DeviceList { get { return _Device; } set { _Device = value; OnPropertyChanged("DeviceList"); } }
        public ObservableCollection<DailyCard> DailyCardList { get { return _DailyCard; } set { _DailyCard = value; OnPropertyChanged("DailyCardList"); } }
        public RelayCommand BrowseCommand { get; set; }
        public RelayCommand UploadCommand { get; set; }
        public RelayCommand ReadLogDateCommand { get; set; }

        public RegisterDailyCardViewModel()
        {
            DailyCardList = new ObservableCollection<DailyCard>();
            GetDeviceList();
            BrowseCommand = new RelayCommand(ExecuteBrowse);
            UploadCommand = new RelayCommand(ExecuteUpload);
            ReadLogDateCommand = new RelayCommand(ReadLogDate);
        }

        private async void ExecuteUpload(object obj)
        {
            try
            {
                foreach (var device in DeviceList)
                {
                    int maxCardId = await GetCardIdFromDb();

                    var zkem = new zkemkeeper.CZKEM();
                    if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                    {
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
                                    if (device.DeviceId == 1)
                                    {
                                        card.Device1 = 1;
                                    }
                                    else if (device.DeviceId == 2)
                                    {
                                        card.Device2 = 1;
                                    }
                                    else if (device.DeviceId == 3)
                                    {
                                        card.Device3 = 1;
                                    }
                                }
                            }
                        }
                    }
                }
                SaveToDb(DailyCardList);
                MessageBox.Show("Cards Uploaded Sucessfully!", "Daily Card Registraion", MessageBoxButton.OK, MessageBoxImage.Information);
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
            catch (Exception ex)
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
                        DailyCardList.Add(new DailyCard { CardNumber = cardNumber, Device1 = 0, Device2 = 0, Device3 = 0 });
                }
            }

        }

        private void GetDeviceList()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                DeviceList = new ObservableCollection<Device>(conn.Query<Device>("select * from DeviceList"));
            }
        }

        private void ReadLogDate(object obj)
        {

            var zkem = new zkemkeeper.CZKEM();
            if (zkem.Connect_Net("192.168.125.233", 4370))
            {
                if (zkem.ReadAllGLogData(zkem.MachineNumber))
                {
                    RegisterDailyCardViewModel ld = new RegisterDailyCardViewModel();
                    while (zkem.GetGeneralLogData(zkem.MachineNumber, ref ld.dwTMachineNumber, ref ld.dwEnrollNumberInt, ref ld.dwEMachineNumber, ref ld.dwVerifyMode, ref ld.dwInOutMode, ref ld.dwYear, ref ld.dwMonth, ref ld.dwDay, ref ld.dwHour, ref ld.dwMinute))
                    {
                        if (ld.dwEnrollNumberInt > short.MaxValue || ld.dwEnrollNumberInt < short.MinValue)
                            continue;
                    }
                }
            }
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
