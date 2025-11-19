using System;
using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters;

/// <summary>
/// Converts a slider value (0-255 for channels, 0-100 for dimmers) and a container height to a pixel height,
/// subtracting a fixed vertical margin of 12px (6px top + 6px bottom).
/// Expects values[0] = slider value (double), values[1] = container height (double).
/// </summary>
public class PercentageHeightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2
            && values[0] is double sliderValue
            && values[1] is double containerHeight)
        {
            double availableHeight = Math.Max(0.0, containerHeight - 12.0);
            double pct = Math.Clamp(sliderValue / 255.0, 0.0, 1.0);
            return pct * availableHeight;
        }

        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
