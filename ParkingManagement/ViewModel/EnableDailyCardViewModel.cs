using Dapper;
using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ParkingManagement.ViewModel
{
    public class EnableDailyCardViewModel : BaseViewModel
    {
        private string _CardNumber;
        private ObservableCollection<Device> _Device;

        public string CardNumber { get { return _CardNumber; } set { _CardNumber = value; OnPropertyChanged("CardNumber"); } }
        public ObservableCollection<Device> DeviceList { get { return _Device; } set { _Device = value; OnPropertyChanged("DeviceList"); } }

        public RelayCommand EnableCardCommand { get; set; }
        public EnableDailyCardViewModel()
        {
            EnableCardCommand = new RelayCommand(ExecuteEnableCard);
            GetDeviceList();
        }

        private void ExecuteEnableCard(object obj)
        {
            if (string.IsNullOrWhiteSpace(CardNumber)) return;
            ReActivateCard();
        }
        private void GetDeviceList()
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                DeviceList = new ObservableCollection<Device>(conn.Query<Device>("select * from DeviceList"));
            }
        }
        private void ReActivateCard()
        {
            foreach (var device in DeviceList)
            {
                var zkem = new zkemkeeper.CZKEM();
                device.DeviceIp = device.DeviceIp.Trim();
                bool isValidIpA = UniversalStatic.ValidateIP(device.DeviceIp);
                if (!isValidIpA)
                {
                    MessageBox.Show($"Invalid Ip: {device.DeviceIp}");
                    continue;
                    //throw new Exception("The Device IP is invalid !!");
                }

                isValidIpA = UniversalStatic.PingTheDevice(device.DeviceIp);
                if (!isValidIpA)
                {
                    MessageBox.Show($"Couldn't connect to device: {device.DeviceIp}");
                    continue;
                    //throw new Exception("The device at " + device.DeviceIp + ":" + device.DevicePort + " did not respond!!");
                }
                if (zkem.Connect_Net(device.DeviceIp, device.DevicePort))
                {
                    var cardId = GetEnrolledIdByCardNumber(CardNumber);

                    zkem.EnableUser(zkem.MachineNumber, cardId, zkem.MachineNumber, 10, true);
                    MessageBox.Show($"Activated at device: {device.DeviceIp}.");
                }
                else
                {
                    MessageBox.Show($"Couldn't connect to device: {device.DeviceIp}. Failed to activate card.");
                }
            }
        }

        private int GetEnrolledIdByCardNumber(string barcode)
        {
            using (SqlConnection conn = new SqlConnection(GlobalClass.TConnectionString))
            {
                var cardId = conn.Query<int>($"select cardid from dailycards where cardnumber='{barcode}'");
                return cardId.FirstOrDefault();
            }
        }
    }
}
