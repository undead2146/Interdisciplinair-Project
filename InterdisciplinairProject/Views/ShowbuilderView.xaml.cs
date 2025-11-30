using InterdisciplinairProject.ViewModels;
using System.Windows.Controls;

namespace InterdisciplinairProject.Views
{

    /// <summary>
    /// Interaction logic for ShowbuilderView.xaml
    /// </summary>
    public partial class ShowbuilderView : UserControl
    {
        public ShowbuilderView(ShowbuilderViewModel showBuilderViewModel)
        {
            InitializeComponent();

            DataContext = showBuilderViewModel;
        }
    }
}
