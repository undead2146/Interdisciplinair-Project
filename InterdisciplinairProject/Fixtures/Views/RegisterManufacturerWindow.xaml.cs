using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.Views
{
    public partial class RegisterManufacturerWindow : Window
    {
        // Publieke Property om de ingevoerde naam op te halen
        public string ManufacturerName { get; private set; } = string.Empty;

        public RegisterManufacturerWindow()
        {
            InitializeComponent();
            NameTextBox.Focus();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            TryRegister();
        }

        private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryRegister();
            }
        }

        private void TryRegister()
        {
            string name = NameTextBox.Text.Trim();

            // Validatie
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("De fabrikantnaam mag niet leeg zijn.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ManufacturerName = name;
            DialogResult = true;
            Close();
        }
    }
}