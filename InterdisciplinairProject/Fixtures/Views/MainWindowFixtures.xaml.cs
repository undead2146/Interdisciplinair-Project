using InterdisciplinairProject.Fixtures.ViewModels;
using System.Windows.Controls;

namespace InterdisciplinairProject.Fixtures.Views
{
    /// <summary>
    /// Interaction logic for MainWindowFixtures.xaml
    /// </summary>
    public partial class MainWindowFixtures : UserControl
    {
        public MainWindowFixtures(MainWindowFixturesViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
