﻿using ParkingManagement.Enums;
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ParkingManagement.Models
{
    public class Device : BaseModel
    {
        private bool _Status;
        private BitmapImage _ImageSource;
        private byte[] _ButtonImage;
        private SolidColorBrush _gridBackground = new SolidColorBrush(Colors.Red);
        private int _EnteredVehicle;
        private string _DeviceTime;
        private string _LastDownloadTime;
        private int _LastDownloadCount;

        public int DeviceId { get; set; }
        public string Devicename { get; set; }
        public string DeviceIp { get; set; }
        public int DevicePort { get; set; }
        public byte VehicleType { get; set; }
        public int IsMemberDevice { get; set; }

        public int EnteredVehicle { get { return _EnteredVehicle; } set { _EnteredVehicle = value; OnPropertyChanged("EnteredVehicle"); } }
        public bool Status { get { return _Status; } set { _Status = value; OnPropertyChanged("Status"); } }
        public int DeviceType { get; set; }
        public string DeviceTime { get { return _DeviceTime; } set { _DeviceTime = value; OnPropertyChanged("DeviceTime"); }  }
        public string LastDownloadTime { get { return _LastDownloadTime; } set { _LastDownloadTime = value; OnPropertyChanged("LastDownloadTime"); } }
        public int LastDownloadCount { get { return _LastDownloadCount; } set { _LastDownloadCount = value; OnPropertyChanged("LastDownloadCount"); } }

        public BitmapImage ImageSource { get { return _ImageSource; } set { _ImageSource = value; OnPropertyChanged("ImageSource"); } }
        public byte[] ButtonImage { get { return _ButtonImage; } set { _ButtonImage = value; OnPropertyChanged("ButtonImage"); } }
        public SolidColorBrush GridBackground
        {
            get { return _gridBackground; }
            set
            {
                _gridBackground = value;
                OnPropertyChanged("GridBackground");
            }
        }

    }
}
