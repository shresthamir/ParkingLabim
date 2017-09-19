using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ParkingManagement.Library.ValueConverter
{
    [ValueConversion(typeof(object), typeof(bool))]
    public class RadioButtonTrueFalseConveter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToString().Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return parameter;
            else
                return null;
        }

    }
}
