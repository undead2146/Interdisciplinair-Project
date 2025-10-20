using System.Windows.Controls;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.Views
{
    /// <summary>
    /// Interaction logic for FixtureListView.xaml
    /// </summary>
    public partial class FixtureListView : UserControl
    {
        public FixtureListView()
        {
            InitializeComponent();
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.FixtureListViewModel vm && vm.OpenFixtureCommand.CanExecute(null))
            {
                vm.OpenFixtureCommand.Execute(null);
            }
        }
    }
}
