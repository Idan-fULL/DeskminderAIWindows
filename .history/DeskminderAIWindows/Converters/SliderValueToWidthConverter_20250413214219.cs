using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskminderAI.Converters
{
    public class SliderValueToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double sliderValue)
            {
                // Assuming the slider is inside a parent container of known width
                // For simplicity, this returns a percentage of the slider width
                double percentage = sliderValue / 60.0; // Assuming maximum is 60
                
                // The actual control has a width of 180, so calculate actual width
                return 180 * percentage;
            }
            
            return 0;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 