using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters;

/// <summary>
/// Compares a numeric value (double) against a threshold supplied via the converter parameter.
/// Returns true when value >= threshold.
/// </summary>
public class GreaterThanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double numericValue && parameter is string thresholdStr)
        {
            if (double.TryParse(thresholdStr, NumberStyles.Number, culture, out double threshold))
            {
                return numericValue >= threshold;
            }
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}