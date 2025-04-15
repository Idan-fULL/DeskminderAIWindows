using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskminderAI.Converters
{
    public class HalfWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                // Return negative half of the width to center the element
                return -width / 2;
            }
            
            return 0;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 