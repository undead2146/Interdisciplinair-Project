using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using InterdisciplinairProject.ViewModels;

namespace InterdisciplinairProject.Views
{
    /// <summary>
    /// Interaction logic for FixtureSettingsView.xaml.
    /// </summary>
    public partial class FixtureSettingsView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixtureSettingsView"/> class.
        /// </summary>
        public FixtureSettingsView()
        {
            Debug.WriteLine("[DEBUG] FixtureSettingsView constructor called");
            InitializeComponent();
            DataContext = new FixtureSettingsViewModel();
            Debug.WriteLine("[DEBUG] FixtureSettingsView DataContext set to FixtureSettingsViewModel");
            Debug.WriteLine("[DEBUG] FixtureSettingsView initialization complete");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[DEBUG] Save button clicked");

            // TODO: Implement save functionality
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[DEBUG] Cancel button clicked");
            
        }
    }
}
