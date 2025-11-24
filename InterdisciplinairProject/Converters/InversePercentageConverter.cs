using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters;

/// <summary>
/// Converts a percentage (double) to the complementary star GridLength (100 - value).
/// Clamps result to [0,100].
/// </summary>
public class InversePercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double sliderValue)
        {
            double inverse = 100.0 - sliderValue;
            inverse = Math.Clamp(inverse, 0.0, 100.0);
            return new GridLength(inverse, GridUnitType.Star);
        }

        return new GridLength(100.0, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}