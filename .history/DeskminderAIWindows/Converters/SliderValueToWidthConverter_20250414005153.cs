using System;
using System.Globalization;
using System.Windows.Data;

namespace DeskminderAI.Converters
{
    public class SliderValueToWidthConverter : IValueConverter
    {
        // The minimum width to display
        private const double MIN_CANVAS_WIDTH = 60;
        
        // The maximum canvas width we'll use for the drag interface
        private const double MAX_CANVAS_WIDTH = 180;
        
        // The maximum minutes value (should match MAX_DURATION in MainWindow.xaml.cs)
        private const double MAX_MINUTES = 120;
        
        // The minimum minutes value
        private const double MIN_MINUTES = 1;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double minutesValue = 1; // Default to 1 minute
            
            // Handle both int and double inputs
            if (value is int intValue)
            {
                minutesValue = intValue;
            }
            else if (value is double doubleValue)
            {
                minutesValue = doubleValue;
            }
            
            // Ensure the value is within range
            minutesValue = Math.Max(MIN_MINUTES, Math.Min(MAX_MINUTES, minutesValue));
            
            // Use a logarithmic scale to make the movement more natural
            // This gives more precision for lower values and less for higher values
            double scaleFactor = Math.Log(minutesValue + 9) / Math.Log(MAX_MINUTES + 9);
            
            // Scale to fit within the available canvas width range
            double width = MIN_CANVAS_WIDTH + (MAX_CANVAS_WIDTH - MIN_CANVAS_WIDTH) * scaleFactor;
            
            return width;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                // Normalize the width to a 0-1 scale
                double normalizedWidth = (width - MIN_CANVAS_WIDTH) / (MAX_CANVAS_WIDTH - MIN_CANVAS_WIDTH);
                
                // Convert from logarithmic scale back to linear
                double minutes = Math.Pow(MAX_MINUTES + 9, normalizedWidth) - 9;
                
                // Ensure it's within valid range
                return (int)Math.Max(MIN_MINUTES, Math.Min(MAX_MINUTES, minutes));
            }
            
            return 1; // Default to 1 minute
        }
    }
} 