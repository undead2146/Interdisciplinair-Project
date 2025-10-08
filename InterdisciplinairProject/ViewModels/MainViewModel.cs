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
                        MessageBox.Show($"Scene '{scene.Name}' succesvol geïmporteerd!",
                            "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Deze scene is al geïmporteerd.",
                            "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show($"Bestand niet gevonden: {ex.Message}",
                        "Bestand niet gevonden", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (InvalidDataException ex)
                {
                    MessageBox.Show($"Ongeldige data in het bestand: {ex.Message}",
                        "Ongeldige data", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ongeldige JSON structuur: {ex.Message}",
                        "JSON Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Fout bij lezen van bestand: {ex.Message}",
                        "Lezingsfout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Onverwachte fout: {ex.Message}",
                        "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
