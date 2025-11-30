using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.Views
{
    /// <summary>
    /// Interaction logic for ExportFixtureWindow.xaml
    /// </summary>
    public partial class ExportFixtureWindow : Window
    {
        public ExportFixtureWindow(string fixtureName)
        {
            InitializeComponent();
            NameTextBox.Text = fixtureName;
            NameTextBox.SelectAll();
            NameTextBox.Focus();
        }

        public string FixtureName => NameTextBox.Text;

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Export_Click(sender, e);
            }
        }
    }
}
