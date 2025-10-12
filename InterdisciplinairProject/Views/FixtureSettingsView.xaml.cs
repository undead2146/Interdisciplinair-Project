using System;
using System.Collections.Generic;
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
    /// Interaction logic for FixtureSettingsView.xaml
    /// </summary>
    public partial class FixtureSettingsView : Window
    {
        public FixtureSettingsView()
        {
            InitializeComponent();
            DataContext = new FixtureSettingsViewModel();
        }
    }
}
