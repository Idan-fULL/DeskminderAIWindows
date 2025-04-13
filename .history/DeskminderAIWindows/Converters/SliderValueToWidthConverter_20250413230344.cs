using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskminderAI.Converters
{
    public class SliderValueToWidthConverter : IValueConverter
    {
        // The maximum canvas width we'll use for the drag interface
        private const double MAX_CANVAS_WIDTH = 220;
        
        // The maximum minutes value (should match MAX_DURATION in MainWindow.xaml.cs)
        private const double MAX_MINUTES = 120;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double minutesValue = 0;
            
            // Handle both int and double inputs
            if (value is int intValue)
            {
                minutesValue = intValue;
            }
            else if (value is double doubleValue)
            {
                minutesValue = doubleValue;
            }
            
            // Calculate width based on percentage of maximum duration
            double percentage = minutesValue / MAX_MINUTES;
            
            // Scale to fit within the available canvas width
            double width = MAX_CANVAS_WIDTH * percentage;
            
            // Ensure we have at least a minimal width for visibility
            return Math.Max(width, 10);
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                double percentage = width / MAX_CANVAS_WIDTH;
                return (int)(percentage * MAX_MINUTES);
            }
            
            return 1; // Default to 1 minute
        }
    }
} 