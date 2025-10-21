using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Show;
using Show.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InterdisciplinairProject.ViewModels
{
    public partial class ShowbuilderViewModel : ObservableObject
    {
        public ObservableCollection<Scene> Scenes { get; } = new();

        [ObservableProperty]
        public Scene? selectedScene;

        [RelayCommand]
        private void ImportScenes()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import Scene",
                Filter = "JSON files (*.json)|*.json",
                Multiselect = false,
            };

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedScenePath = openFileDialog.FileName;

                    Scene scene = SceneExtractor.ExtractScene(selectedScenePath);
                    if (!Scenes.Any(s => s.Id == scene.Id))
                    {
                        Scenes.Add(scene);
                        MessageBox.Show($"Scene '{scene.Name}' imported successfully!", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("This scene has already been imported.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SceneSelectionChanged(Scene selectedScene)
        {
            SelectedScene = selectedScene;
        }
    }
}
