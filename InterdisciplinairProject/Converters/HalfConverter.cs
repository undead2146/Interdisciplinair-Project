using System.Globalization;
using System.Windows.Data;

namespace InterdisciplinairProject.Converters;

/// <summary>
/// Converts a value by multiplying it by 0.5. Used by ScenebuilderView to limit the max width of the left panel to half the window width.
/// </summary>
public class HalfConverter : IValueConverter
{
    /// <summary>
    /// Converts the value by multiplying by 0.5.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return d * 0.5;
        }

        return 0;
    }

    /// <summary>
    /// Not implemented.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
