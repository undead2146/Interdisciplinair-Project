using System.Windows.Controls;
using InterdisciplinairProject.ViewModels;

namespace InterdisciplinairProject.Views
{
    public partial class ScenebuilderView : UserControl
    {
        public ScenebuilderView()
        {
            InitializeComponent();
            DataContext = new ScenebuilderViewModel();
        }
    }
}