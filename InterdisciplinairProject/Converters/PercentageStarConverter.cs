using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters;

/// <summary>
/// Converts a percentage value (double) to a GridLength with GridUnitType.Star.
/// The converter preserves the incoming numeric value as the star weight.
/// </summary>
public class PercentageStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double sliderValue)
        {
            double star = Math.Max(0.0, sliderValue);
            return new GridLength(star, GridUnitType.Star);
        }

        return new GridLength(0.0, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}