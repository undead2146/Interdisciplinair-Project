using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace InterdisciplinairProject.Views
{
    public partial class SceneControl : UserControl
    {
        public SceneControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }

    public class PercentageHeightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is double sliderValue && values[1] is double containerHeight)
            {
                // Calculate height as percentage of container (slider is 0-100)
                // Subtract margin (6px top + 6px bottom = 12px total)
                double availableHeight = containerHeight - 12;
                return (sliderValue / 100.0) * availableHeight;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GreaterThanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double sliderValue && parameter is string thresholdStr && double.TryParse(thresholdStr, out double threshold))
            {
                return sliderValue >= threshold;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentageStarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double sliderValue)
            {
                // Return star value (e.g., "1*" for 100%, "0.5*" for 50%)
                return new GridLength(sliderValue, GridUnitType.Star);
            }
            return new GridLength(0, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InversePercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double sliderValue)
            {
                // Return inverse star value (100 - value)
                return new GridLength(100 - sliderValue, GridUnitType.Star);
            }
            return new GridLength(100, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
