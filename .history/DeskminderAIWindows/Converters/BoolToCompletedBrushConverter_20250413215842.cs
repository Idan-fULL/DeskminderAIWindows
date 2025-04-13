using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WPFColor = System.Windows.Media.Color;

namespace DeskminderAI.Converters
{
    public class BoolToCompletedBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCompleted)
            {
                return isCompleted 
                    ? new LinearGradientBrush(WPFColor.FromRgb(0x55, 0x55, 0x55), WPFColor.FromRgb(0x44, 0x44, 0x44), 45) 
                    : new LinearGradientBrush(WPFColor.FromRgb(0x4C, 0xAF, 0x50), WPFColor.FromRgb(0x2d, 0x68, 0x2f), 45);
            }
            
            return new SolidColorBrush(Colors.Gray);
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 