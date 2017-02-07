using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ParkingManagement.Library.ValueConverter
{
    class DayWiseRateConverter:IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            IEnumerable<RateDetails> AllRates = values[0] as IEnumerable<RateDetails>;
            byte DayId = (byte)values[1];
            byte VTypeID = System.Convert.ToByte(values[2]);
            return new ObservableCollection<RateDetails>(AllRates.Where(x => x.Day == DayId && x.VehicleType == VTypeID).OrderBy(x => x.BeginTime));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
