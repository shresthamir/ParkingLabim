using Newtonsoft.Json;
using ParkingManagement.Library.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingManagement.Models
{
    class MembershipScheme : BaseModel
    {
        private int _SchemeId;
        private string _SchemeName;
        private bool _ValidOnWeekends;
        private bool _ValidOnHolidays;
        private int _ValidityPeriod;
        private double _Discount;
        private int _Limit;
        private ObservableCollection<ValidHour> _ValidHoursList;

        public int SchemeId { get { return _SchemeId; } set { _SchemeId = value; OnPropertyChanged("SchemeId"); } }
        public string SchemeName { get { return _SchemeName; } set { _SchemeName = value; OnPropertyChanged("SchemeName"); } }
        public bool ValidOnWeekends { get { return _ValidOnWeekends; } set { _ValidOnWeekends = value; OnPropertyChanged("ValidOnWeekends"); } }
        public bool ValidOnHolidays { get { return _ValidOnHolidays; } set { _ValidOnHolidays = value; OnPropertyChanged("ValidOnHolidays"); } }
        public int ValidityPeriod { get { return _ValidityPeriod; } set { _ValidityPeriod = ValidityPeriod; OnPropertyChanged("ValidityPeriod"); } }
        public double Discount { get { return _Discount; } set { _Discount = value; OnPropertyChanged("Discount"); } }
        public int Limit { get { return _Limit; } set { _Limit = value; OnPropertyChanged("Limit"); } }
        public string ValidHours { get { return JsonConvert.SerializeObject(ValidHoursList); } set { ValidHoursList = JsonConvert.DeserializeObject<ObservableCollection<ValidHour>>(value); } }
        public ObservableCollection<ValidHour> ValidHoursList { get { return _ValidHoursList; } set { _ValidHoursList = value; OnPropertyChanged("ValidHoursList"); } }
        
    }

    class ValidHour : BaseModel
    {
        private TimeSpan _Start;
        private TimeSpan _End;

        public TimeSpan Start { get { return _Start; } set { _Start = value; OnPropertyChanged("Start"); } }
        public TimeSpan End { get { return _End; } set { _End = value; OnPropertyChanged("End"); } }
        public string test { get; set; }
        public ValidHour()
        {
            Start = new TimeSpan(0, 0, 0);
            End = new TimeSpan(23,59,59);
        }
    }
}
