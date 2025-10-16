using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
