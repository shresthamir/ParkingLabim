using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ParkingManagement.Library.ValueConverter
{
    class RateTimeLineWidthConverter:IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DateTime BeginTime = (DateTime)values[0];
            DateTime EndTime = (DateTime)values[1];
            return (EndTime - BeginTime).TotalMinutes * (GlobalClass.RateTimeLinePeriodWidth / 120);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
