using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using InterdisciplinairProject.ViewModels;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
