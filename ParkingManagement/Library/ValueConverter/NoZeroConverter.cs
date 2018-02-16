﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ParkingManagement.Library.ValueConverter
{
    public class NoZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var result = GParse.ToDecimal(value);
            return (result > 0) ? ((parameter == null) ? result.ToString("#0.00") : result.ToString()) : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType == typeof(Int32))
                return GParse.ToInteger(value.ToString());
            return GParse.ToDecimal(value.ToString());
        }
    }
}
