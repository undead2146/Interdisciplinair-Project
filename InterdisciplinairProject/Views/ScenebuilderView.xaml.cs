using System.Windows.Controls;
using System.Windows.Input;
using InterdisciplinairProject.ViewModels;

namespace InterdisciplinairProject.Views
{
    public partial class ScenebuilderView : UserControl
    {
        public ScenebuilderView()
        {
            InitializeComponent();
        }

        private async void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ScenebuilderViewModel viewModel && viewModel.SelectedScene != null)
            {
                await viewModel.OpenSceneEditor();
            }
        }
    }
}