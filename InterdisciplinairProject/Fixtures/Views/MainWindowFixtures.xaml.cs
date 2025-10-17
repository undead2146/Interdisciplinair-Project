using InterdisciplinairProject.Fixtures.ViewModels;
using System.Windows;

namespace InterdisciplinairProject.Fixtures.Views
{
    /// <summary>
    /// Interaction logic for MainWindowFixtures.xaml
    /// </summary>
    public partial class MainWindowFixtures : Window
    {
        public MainWindowFixtures()
        {
            InitializeComponent();
            DataContext = new MainWindowFixturesViewModel();
        }
    }
}
