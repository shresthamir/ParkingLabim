using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;

namespace ParkingManagement.Models
{
    [Table(Name = "Party")]
    public class Party : BaseModel
    {
        private int _Party_ID;
        private string _PartyName;
        private int _Rate_ID;
        private int _UID;
        private RateMaster _Rate;


        [Column(IsPrimaryKey = true)]
        public int Party_ID { get { return _Party_ID; } set { _Party_ID = value; OnPropertyChanged("Party_ID"); } }
        [Column]
        public string PartyName { get { return _PartyName; } set { _PartyName = value; OnPropertyChanged("PartyName"); } }
        [Column]
        public int Rate_ID { get { return _Rate_ID; } set { _Rate_ID = value; OnPropertyChanged("Rate_ID"); } }
        [Column]
        public int UID { get { return _UID; } set { _UID = value; OnPropertyChanged("UID"); } }
        public RateMaster Rate { get { return _Rate; } set { _Rate = value; OnPropertyChanged("Rate"); } }

        public Party()
        {
            this.UID = GlobalClass.User.UID;
        }
    }
}
