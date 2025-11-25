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
    private const double VerticalMargin = 12.0;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2)
            return 0.0;

        // New form expects 4 bindings: Value, Minimum, Maximum, ContainerHeight
        if (values.Length >= 4)
        {
            if (!TryToDouble(values[0], culture, out double sliderValue) ||
                !TryToDouble(values[1], culture, out double min) ||
                !TryToDouble(values[2], culture, out double max) ||
                !TryToDouble(values[3], culture, out double containerHeight))
            {
                return 0.0;
            }

            if (double.IsNaN(sliderValue) || double.IsInfinity(sliderValue) ||
                double.IsNaN(min) || double.IsInfinity(min) ||
                double.IsNaN(max) || double.IsInfinity(max) ||
                double.IsNaN(containerHeight) || double.IsInfinity(containerHeight))
            {
                return 0.0;
            }

            // Prevent divide-by-zero or inverted ranges
            if (max <= min)
                return 0.0;

            double availableHeight = Math.Max(0.0, containerHeight - VerticalMargin);
            if (availableHeight <= 0.0)
                return 0.0;

            double pct = (sliderValue - min) / (max - min);
            pct = Math.Clamp(pct, 0.0, 1.0);
            return pct * availableHeight;
        }

        // Back-compat: [Value, ContainerHeight] — preserve previous heuristic but safer
        if (values.Length == 2)
        {
            if (!TryToDouble(values[0], culture, out double sliderValue) ||
                !TryToDouble(values[1], culture, out double containerHeight))
            {
                return 0.0;
            }

            if (double.IsNaN(sliderValue) || double.IsInfinity(sliderValue) ||
                double.IsNaN(containerHeight) || double.IsInfinity(containerHeight))
            {
                return 0.0;
            }

            double availableHeight = Math.Max(0.0, containerHeight - VerticalMargin);
            if (availableHeight <= 0.0)
                return 0.0;

            double pct;
            if (sliderValue >= 0.0 && sliderValue <= 1.0)
                pct = sliderValue;
            else if (sliderValue >= 0.0 && sliderValue <= 100.0)
                pct = sliderValue / 100.0;
            else
                pct = sliderValue / 255.0;

            pct = Math.Clamp(pct, 0.0, 1.0);
            return pct * availableHeight;
        }

        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static bool TryToDouble(object input, CultureInfo culture, out double result)
    {
        result = 0.0;
        if (input == null)
            return false;

        try
        {
            // If it's already a double, cast directly
            if (input is double d)
            {
                result = d;
                return true;
            }

            // Use Convert to support int, byte, float, decimal, etc.
            result = System.Convert.ToDouble(input, culture);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
