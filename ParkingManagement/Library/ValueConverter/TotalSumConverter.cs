using ParkingManagement.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ParkingManagement.Library.ValueConverter
{
    public class TotalSumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            var sales = value as IEnumerable<object>;
            if (sales == null)
                return "0.00";
            decimal sum = 0;

            foreach (var u in sales)
            {
                DataItem obj = u as DataItem;
                sum += GParse.ToDecimal(obj.GetType().GetProperty(parameter.ToString()).GetValue(obj, null));
            }
            return sum.ToString("#0.00");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
