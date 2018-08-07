using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace ParkingManagement.Library.ValueConverter
{
    public class DateFromatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime mod = DateTime.Today;
            if (DateTime.TryParse(value.ToString(), out mod))
                return mod.ToString("MM/dd/yyyy");
            else return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    
    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateToMitiConverter : IValueConverter
    {
        DateConverter nepDate;
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            if (((DateTime)value) < new DateTime(1882, 4, 13))
                return "";
            nepDate = new DateConverter(GlobalClass.TConnectionString);
            return nepDate.CBSDate((DateTime)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return DateTime.Today;
            nepDate = new DateConverter(GlobalClass.TConnectionString);
            return nepDate.CADDate(value.ToString());
        }
    }

    [ValueConversion(typeof(decimal), typeof(decimal))]
    public class NoteToAmountConverter :  IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return GParse.ToDecimal(value) * GParse.ToDecimal(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return GParse.ToDecimal(value) / GParse.ToDecimal(parameter);
        }
    }


    [ValueConversion(typeof(decimal), typeof(bool))]
    public class IsNotZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return GParse.ToDecimal(value) > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(string))]
    public class BoolToContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return GParse.ToBool(value) ? "Save" : "New";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
