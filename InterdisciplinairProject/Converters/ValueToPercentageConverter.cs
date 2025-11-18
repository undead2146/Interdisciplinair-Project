using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters;

/// <summary>
/// Converts a DMX value (0-255) to a percentage (0-100).
/// </summary>
public class ValueToPercentageConverter : IValueConverter
{
    /// <summary>
    /// Converts a DMX value to percentage.
    /// </summary>
    /// <param name="value">The DMX value (0-255).</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Optional parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>The percentage value (0-100).</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is byte dmxValue)
        {
            return Math.Round(dmxValue / 2.55, 0);
        }

        return 0.0;
    }

    /// <summary>
    /// Converts back from percentage to DMX value.
    /// </summary>
    /// <param name="value">The percentage value (0-100).</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Optional parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>The DMX value (0-255).</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percentage)
        {
            return (byte)Math.Round(percentage * 2.55, 0);
        }

        return (byte)0;
    }
}