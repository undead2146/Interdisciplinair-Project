using System;
using System.Globalization;
using System.Windows.Data;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Converters
{
    /// <summary>
    /// Converts a Fixture to its end DMX address.
    /// </summary>
    public class FixtureEndAddressConverter : IValueConverter
    {
        /// <summary>
        /// Converts a Fixture to its end DMX address.
        /// </summary>
        /// <param name="value">The Fixture object.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The converter parameter.</param>
        /// <param name="culture">The culture info.</param>
        /// <returns>The end DMX address as an integer.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Fixture fixture)
            {
                return fixture.StartAddress + fixture.ChannelCount - 1;
            }

            return 1;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>Not supported.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
