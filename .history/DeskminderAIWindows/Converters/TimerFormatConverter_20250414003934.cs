using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DeskminderAI.Converters
{
    public class TimerFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int minutes)
            {
                if (minutes == 1)
                    return "דקה אחת";
                else
                    return $"{minutes} דקות";
            }
            
            return "0 דקות";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                if (strValue == "דקה אחת")
                    return 1;
                
                string digitsOnly = new string(strValue.Where(c => char.IsDigit(c)).ToArray());
                if (int.TryParse(digitsOnly, out int result))
                    return result;
            }
            
            return 0;
        }
    }
} 