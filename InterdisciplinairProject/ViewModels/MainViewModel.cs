using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Show;
using Show.Model;
using System.Collections.ObjectModel;
using System.Windows;

namespace InterdiscplinairProject.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "InterdisciplinairProject - DMX Lighting Control";

        [ObservableProperty]
        private string? selectedScenePath;

        public ObservableCollection<Scene> Scenes { get; } = new();

        [RelayCommand]
        private void ImportScenes()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Scene",
                Filter = "JSON files (*.json)|*.json",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedScenePath = openFileDialog.FileName;

                try
                {
                    Scene scene = SceneExtractor.ExtractScene(selectedScenePath);
                    Scenes.Add(scene);

                    MessageBox.Show($"Scene '{scene.Name}' succesvol geïmporteerd!", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fout bij importeren: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
