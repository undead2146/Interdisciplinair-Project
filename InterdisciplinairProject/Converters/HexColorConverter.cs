using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace InterdisciplinairProject.Converters;

/// <summary>
/// Converts a hex color string to a SolidColorBrush.
/// </summary>
public class HexColorConverter : IValueConverter
{
    /// <summary>
    /// Converts a hex color string to a SolidColorBrush.
    /// </summary>
    /// <param name="value">The hex color string.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Optional parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>A SolidColorBrush with the specified color.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hexColor && !string.IsNullOrWhiteSpace(hexColor))
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor));
            }
            catch
            {
                return new SolidColorBrush(Colors.Black);
            }
        }

        return new SolidColorBrush(Colors.Black);
    }

    /// <summary>
    /// Converts back (not implemented).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">Optional parameter.</param>
    /// <param name="culture">The culture info.</param>
    /// <returns>Not implemented.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
