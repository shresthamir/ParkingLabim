using ParkingManagement.Library.Helpers;
using System;

namespace ParkingManagement.Models
{
    class ParkingEntranceClose : BaseModel
    {
        private bool _Close;
        private string _Remarks;

        public int PID { get; set; }
        public DateTime InDate { get; set; }
        public string InMiti { get; set; }
        public string VehicleType { get; set; }
        public string PlateNo { get; set; }
        public string Barcode { get; set; }
        public string InTime { get; set; }
        public bool Close { get { return _Close; } set { _Close = value; OnPropertyChanged("Close"); } }
        public byte FYID { get; set; }
        public string Remarks { get { return _Remarks; } set { _Remarks = value; OnPropertyChanged("Remarks"); } }
    }
}
