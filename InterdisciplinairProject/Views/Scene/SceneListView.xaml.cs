using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using InterdisciplinairProject.ViewModels;

namespace InterdisciplinairProject.Views.Scene
{
    public partial class SceneListView : UserControl
    {
        public SceneListView()
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