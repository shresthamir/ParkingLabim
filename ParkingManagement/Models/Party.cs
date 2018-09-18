using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;

namespace ParkingManagement.Models
{
    public class Party : BaseModel
    {
        private string _Name;
        private string _Address;
        private string _PAN;

        public string Name { get { return _Name; } set { _Name = value; OnPropertyChanged("Name"); } }
        public string Address { get { return _Address; } set { _Address = value; OnPropertyChanged("Address"); } }
        public string PAN { get { return _PAN; } set { _PAN = value; OnPropertyChanged("PAN"); } }

        public Party()
        {
        }
    }
}
