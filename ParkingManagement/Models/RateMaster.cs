using ParkingManagement.Library;
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Dapper;
namespace ParkingManagement.Models
{
    [Table(Name = "RateMaster")]
    public class TRateMaster : BaseModel
    {
        int _Rate_ID;
        string _RateDescription;
        bool _IsDefault;
        int _UID;

        [Column(IsPrimaryKey = true)]
        public int Rate_ID { get { return _Rate_ID; } set { _Rate_ID = value; OnPropertyChanged("Rate_ID"); } }
        [Column]
        public string RateDescription { get { return _RateDescription; } set { _RateDescription = value; OnPropertyChanged("RateDescription"); } }
        [Column]
        public bool IsDefault { get { return _IsDefault; } set { _IsDefault = value; OnPropertyChanged("IsDefault"); } }
        [Column]
        public int UID { get { return _UID; } set { _UID = value; OnPropertyChanged("UID"); } }

        public bool Save(SqlTransaction tran)
        {
            string strSql = "INSERT INTO RateMaster (Rate_ID, RateDescription, IsDefault, [UID]) VALUES (@Rate_ID, @RateDescription, @IsDefault, @UID)";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }

        public bool Update(SqlTransaction tran)
        {
            string strSql = "UPDATE RateMaster SET RateDescription = @RateDescription,IsDefault = @IsDefault, [UID] = @UID  WHERE Rate_ID = @Rate_ID";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }
    }

    [Table(Name = "RateDetails")]
    public class TRateDetails : BaseModel
    {
        int _Rate_ID;
        byte _VehicleTypeID;
        DateTime _Start = GlobalClass.BeginTime;
        DateTime _End = GlobalClass.EndTime;
        decimal _CashRate;
        decimal _PartyRate;
        int _UID;
        bool _BulkCharge;
        private byte _Day;

        [Column]
        public int Rate_ID { get { return _Rate_ID; } set { _Rate_ID = value; OnPropertyChanged("Rate_ID"); } }
        [Column]
        public byte VehicleType { get { return _VehicleTypeID; } set { _VehicleTypeID = value; OnPropertyChanged("VehicleType"); } }
        [Column]
        public byte Day { get { return _Day; } set { _Day = value; OnPropertyChanged("Day"); } }
        [Column]
        public DateTime BeginTime
        {
            get { return _Start; }
            set
            {
                if (value > GlobalClass.EndTime)
                    _Start = value.Subtract(new TimeSpan(1, 0, 0, 0));
                else if (value < GlobalClass.BeginTime)
                    _Start = value.AddDays(1);
                else
                    _Start = value;
                OnPropertyChanged("BeginTime");
            }
        }
        [Column]
        public DateTime EndTime
        {
            get { return _End; }
            set
            {
                if (value > GlobalClass.EndTime)
                    _End = value.Subtract(new TimeSpan(1, 0, 0, 0));
                else if (value < GlobalClass.BeginTime)
                    _End = value.AddDays(1);
                else
                    _End = value;
                OnPropertyChanged("EndTime");
            }
        }
        [Column]
        public decimal Rate { get { return _CashRate; } set { _CashRate = value; OnPropertyChanged("Rate"); } }

        [Column]
        public bool IsFixed { get { return _BulkCharge; } set { _BulkCharge = value; OnPropertyChanged("IsFixed"); } }


        public bool Save(SqlTransaction tran)
        {
            string strSql = "INSERT INTO RATEDETAILS (Rate_ID, VehicleType, [Day], BeginTime, EndTime, Rate, IsFixed) VALUES(@Rate_ID, @VehicleType, @Day, @BeginTime, @EndTime, @Rate, @IsFixed)";
            return tran.Connection.Execute(strSql, param: this, transaction: tran) == 1;
        }
    }

    public class LRateMaster:TRateMaster
    {
        ObservableCollection<TRateDetails> _Rates;
        public ObservableCollection<TRateDetails> Rates { get { return _Rates; } set { _Rates = value; OnPropertyChanged("Rates"); } }
    }

    public class RateMaster : TRateMaster
    {
        ObservableCollection<RateDetails> _Rates;
        public ObservableCollection<RateDetails> Rates { get { return _Rates; } set { _Rates = value; OnPropertyChanged("Rates"); } }

        public RateMaster()
        {
            Rates = new ObservableCollection<RateDetails>();
            UID = GlobalClass.User.UID;
        }
    }

    public class RateDetails : TRateDetails
    {
        VehicleType _VType;
        private Models.Day _DayOfWeek;

        public VehicleType VType { get { return _VType; } set { _VType = value; OnPropertyChanged("VType"); } }
        public Day DayOfWeek { get { return _DayOfWeek; } set { _DayOfWeek = value; OnPropertyChanged("DayOfWeek"); } }


        // 0 = Normal; 1 = Edit
        public byte Mode { get; set; }
        public RateDetails()
        {
            //this.PropertyChanged += RateDetails_PropertyChanged;
        }

        //void RateDetails_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if(Mode == 1 && e.PropertyName="EndTime")
        //    {
        //        OnPropertyChanged()
        //    }
        //}
    }

    public class Day : BaseModel
    {
        private byte _DayId;
        private string _DayName;
        private bool _IsChecked;
        private bool _IsEnabled = true;
        public byte DayId { get { return _DayId; } set { _DayId = value; OnPropertyChanged("DayId"); } }
        public string DayName { get { return _DayName; } set { _DayName = value; OnPropertyChanged("DayName"); } }
        public bool IsChecked { get { return _IsChecked; } set { _IsChecked = value; OnPropertyChanged("IsChecked"); } }
        public bool IsEnabled { get { return _IsEnabled; } set { _IsEnabled = value; OnPropertyChanged("IsEnabled"); } }
    }
}
