using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TodoApp
{
    public class PriorityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() == "High")
            {
                // Very light red for high priority rows
                return new SolidColorBrush(Color.FromArgb(40, 255, 0, 0));
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    // 1. Logic for the Green/Blue/Red status bubbles
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int pct)
            {
                if (pct == 100) return Brushes.Green;
                if (pct > 0) return Brushes.DeepSkyBlue;
                return Brushes.OrangeRed;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    // 2. Logic to turn rows Red if they are past the End Date
    public class OverdueColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskItem task && task.PercentComplete < 100 && !string.IsNullOrEmpty(task.EndDate))
            {
                if (DateTime.TryParse(task.EndDate, out DateTime end))
                {
                    if (end.Date < DateTime.Now.Date)
                    {
                        // Returns a very light red background
                        return new SolidColorBrush(Color.FromRgb(255, 230, 230));
                    }
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}