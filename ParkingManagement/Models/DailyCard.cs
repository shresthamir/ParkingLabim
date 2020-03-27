using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Models
{
    public class DailyCard : BaseModel
    {
        private int _Device1;
        private string _CardNumber;
        private int _Device2;
        private int _Device3;
        private ObservableCollection<Device> _DeviceList;

        public int CardId { get; set; }
        public string CardNumber { get { return _CardNumber; } set { _CardNumber = value; OnPropertyChanged("CardNumber"); } }
        public ObservableCollection<Device> DeviceList { get {return _DeviceList; } set { _DeviceList = value; OnPropertyChanged("DeviceList"); } }
        //public int Device1 { get { return _Device1; } set { _Device1 = value; OnPropertyChanged("Device1"); } }
        //public int Device2 { get { return _Device2; } set { _Device2 = value; OnPropertyChanged("Device2"); } }
        //public int Device3 { get { return _Device3; } set { _Device3 = value; OnPropertyChanged("Device3"); } }
        
    }
}
