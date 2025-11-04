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
            Debug.WriteLine("[DEBUG] FixtureSettingsView initialization complete");
        }

        // Called when the "Save" button in the XAML is clicked.
        // Replace the body with actual save logic or invoke a ViewModel command.
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[DEBUG] Save button clicked");

            // TODO: Implement save functionality
        }

        // Called when the "Cancel" button in the XAML is clicked.
        // Replace the body with cancel/reset/navigation logic as appropriate.
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[DEBUG] Cancel button clicked");
        }
    }
}
