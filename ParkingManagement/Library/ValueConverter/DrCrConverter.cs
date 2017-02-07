using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ParkingManagement.Library.ValueConverter
{
    [ValueConversion(typeof(decimal), typeof(string))]
    public class DrCrConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = GParse.ToDecimal(value);
            if (result == 0)
                return "0.00";
            return (result > 0) ? result.ToString("#0.00") + " Dr" : (-result).ToString("#0.00") + " Cr";

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return GParse.ToDecimal(value.ToString().Substring(0, value.ToString().Length - 3));
        }
    }
}
