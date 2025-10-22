using System.Windows;

namespace InterdisciplinairProject.Fixtures.Views
{
    public partial class ExportFixtureWindow : Window
    {
        public string FixtureName { get; set; }

        public ExportFixtureWindow(string currentName)
        {
            InitializeComponent();
            FixtureName = currentName;
            NameTextBox.Text = currentName;
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            FixtureName = NameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(FixtureName))
            {
                MessageBox.Show("Fixture name cannot be empty.");
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
