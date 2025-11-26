using System;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

// Dit moet overeenkomen met xmlns:conv="clr-namespace:InterdisciplinairProject.Fixtures.Converters"
namespace InterdisciplinairProject.Fixtures.Converters
{
    // Zet True naar Visible, False naar Collapsed
    public class ManufacturerBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    // Zet False naar Visible, True naar Collapsed
    public class ManufacturerInverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && !b)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    // Bepaalt de kleur van de 'Verwijderen' knop (bijv. rood als het item in gebruik is)
    public class ManufacturerDeleteButtonForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // De IsEmpty property wordt gebruikt in de binding in XAML.
            // In dit voorbeeld is de knop ingeschakeld (IsEnabled) als IsEmpty=True
            // De foreground is hier puur cosmetisch.
            if (value is bool isEmpty && isEmpty)
            {
                // Kleur voor een item dat nog niet opgeslagen is (kan veilig verwijderd worden)
                return new SolidColorBrush(Colors.Red);
            }
            else
            {
                // Kleur voor een opgeslagen item (kan in gebruik zijn, dus neutrale kleur)
                // Dit zorgt ervoor dat de knop 'gedimd' lijkt als hij Disabled=False is maar niet IsEmpty=True.
                // U zou hier de kleur kunnen baseren op de IsInUse property van de ManufacturerItem.
                return new SolidColorBrush(Colors.DarkGray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}