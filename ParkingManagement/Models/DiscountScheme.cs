using Newtonsoft.Json;
using ParkingManagement.Library.Helpers;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using Dapper;
using System;

namespace ParkingManagement.Models
{
    class DiscountScheme : BaseModel
    {
        private int _SchemeId;
        private string _SchemeName;
        private bool _ValidOnWeekends;
        private bool _ValidOnHolidays;
        private int _MaxHrs;
        private decimal _Discount;
        private int _MinHrs;
        private ObservableCollection<ValidHour> _ValidHoursList;
        private ObservableCollection<DisAmount> _DisAmountList;
        private DateTime _ExpiryDate = DateTime.Today;

        public DateTime ExpiryDate { get { return _ExpiryDate; } set { _ExpiryDate = value; OnPropertyChanged("ExpiryDate"); } }
        public int SchemeId { get { return _SchemeId; } set { _SchemeId = value; OnPropertyChanged("SchemeId"); } }
        public string SchemeName { get { return _SchemeName; } set { _SchemeName = value; OnPropertyChanged("SchemeName"); } }
        public bool ValidOnWeekends { get { return _ValidOnWeekends; } set { _ValidOnWeekends = value; OnPropertyChanged("ValidOnWeekends"); } }
        public bool ValidOnHolidays { get { return _ValidOnHolidays; } set { _ValidOnHolidays = value; OnPropertyChanged("ValidOnHolidays"); } }
        public decimal DiscountPercent { get { return _Discount; } set { _Discount = value; OnPropertyChanged("DiscountPercent"); } }
        public string DiscountAmount { get { return JsonConvert.SerializeObject(DisAmountList); } set { DisAmountList = JsonConvert.DeserializeObject<ObservableCollection<DisAmount>>(value); } }
        public int MinHrs { get { return _MinHrs; } set { _MinHrs = value; OnPropertyChanged("MinHrs"); } }
        public int MaxHrs { get { return _MaxHrs; } set { _MaxHrs = value; OnPropertyChanged("MaxHrs"); } }
        public string ValidHours { get { return JsonConvert.SerializeObject(ValidHoursList); } set { ValidHoursList = JsonConvert.DeserializeObject<ObservableCollection<ValidHour>>(value); } }
        public ObservableCollection<ValidHour> ValidHoursList { get { return _ValidHoursList; } set { _ValidHoursList = value; OnPropertyChanged("ValidHoursList"); } }
        public ObservableCollection<DisAmount> DisAmountList { get { return _DisAmountList; } set { _DisAmountList = value; OnPropertyChanged("DisAmountList"); } }
        public override bool Save(SqlTransaction tran)
        {
            return tran.Connection.Execute("INSERT INTO DiscountScheme(SchemeId, SchemeName, ValidOnWeekends, ValidOnHolidays, DiscountPercent, DiscountAmount, ValidHours, MinHrs, MaxHrs, ExpiryDate) VALUES (@SchemeId, @SchemeName, @ValidOnWeekends, @ValidOnHolidays, @DiscountPercent, @DiscountAmount, @ValidHours, @MinHrs, @MaxHrs, @ExpiryDate)", this, tran) == 1;
        }

        public override bool Update(SqlTransaction tran)
        {
            return tran.Connection.Execute("UPDATE DiscountScheme SET SchemeName = @SchemeName, ValidOnWeekends = @ValidOnWeekends, ValidOnHolidays = @ValidOnHolidays, DiscountPercent = @DiscountPercent, DiscountAmount = @DiscountAmount, ValidHours = @ValidHours, MinHrs = @MinHrs, MaxHrs = @MaxHrs, ExpiryDate = @ExpiryDate WHERE SchemeId = @SchemeId", this, tran) == 1;
        }

        public override bool Delete(SqlTransaction tran)
        {
            return tran.Connection.Execute("DELETE FROM DiscountScheme WHERE SchemeId = @SchemeId", this, tran) == 1;
        }
    }

    class DisAmount : BaseModel
    {
        private decimal _Amount;
        private string _VType;
        private byte _VTypeID;

        public byte VTypeID { get { return _VTypeID; } set { _VTypeID = value; OnPropertyChanged("_VTypeID"); } }
        public string VType { get { return _VType; } set { _VType = value; OnPropertyChanged("VType"); } }
        public decimal Amount { get { return _Amount; } set { _Amount = value; OnPropertyChanged("Amount"); } }
    }
}
