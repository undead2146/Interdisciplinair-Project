using InterdisciplinairProject.Fixtures.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace InterdisciplinairProject.Fixtures.Views
{
    /// <summary>
    /// Interaction logic for FixtureListView.xaml
    /// </summary>
    public partial class FixtureListView : Window
    {
        public FixtureListView()
        {
            InitializeComponent();
            DataContext = new FixtureListViewModel();
        }
    }
}
