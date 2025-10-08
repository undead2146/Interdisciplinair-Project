using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Show;
using Show.Model;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
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
                    if (!Scenes.Any(s => s.Id == scene.Id))
                    {
                        Scenes.Add(scene);
                        MessageBox.Show($"Scene '{scene.Name}' imported successfully!",
                            "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("This scene has already been imported.",
                            "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show($"File not found: {ex.Message}",
                        "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (InvalidDataException ex)
                {
                    MessageBox.Show($"Invalid data in the file: {ex.Message}",
                        "Invalid Data", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Invalid JSON structure: {ex.Message}",
                        "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error reading the file: {ex.Message}",
                        "Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
