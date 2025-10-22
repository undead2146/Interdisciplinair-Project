using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Show;
using Show.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using InterdisciplinairProject.Views; // 👈 Needed for CreateShowWindow

namespace InterdisciplinairProject.ViewModels
{
    public partial class ShowbuilderViewModel : ObservableObject
    {

        private Shows _show = new Shows();
        private string? _currentShowPath;

        public ObservableCollection<Scene> Scenes { get; } = new();

        [ObservableProperty]
        private Scene? selectedScene;

        [ObservableProperty]
        private string? currentShowId;

        [ObservableProperty]
        private string? currentShowName;

        [ObservableProperty]
        private string? message;

        // ============================================================
        // CREATE SHOW
        // ============================================================
        [RelayCommand]
        private void CreateShow()
        {
            // Open the create show window
            var window = new CreateShowWindow();
            var vm = (CreateShowViewModel)window.DataContext;

            bool? result = window.ShowDialog();
            if (result == true && !string.IsNullOrWhiteSpace(vm.ShowName))
            {
                // Update current show
                CurrentShowName = vm.ShowName;
                Scenes.Clear();

                _show = new Shows
                {
                    Name = vm.ShowName,
                    Scenes = new List<Scene>()
                };

                _currentShowPath = null;

                Message = $"Nieuwe show '{vm.ShowName}' aangemaakt!";
            }
        }

        // ============================================================
        // IMPORT SCENES
        // ============================================================
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
                        Message = $"Scene '{scene.Name}' imported successfully!";
                    }
                    else
                    {
                        Message = "This scene has already been imported.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // SCENE SELECTION
        // ============================================================
        [RelayCommand]
        private void SceneSelectionChanged(Scene selectedScene)
        {
            SelectedScene = selectedScene;
        }

        // ============================================================
        // SAVE AS
        // ============================================================
        [RelayCommand]
        private void SaveAs()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Show As",
                    Filter = "JSON files (*.json)|*.json",
                    DefaultExt = ".json",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    FileName = string.IsNullOrWhiteSpace(CurrentShowName)
                        ? "NewShow.json"
                        : $"{CurrentShowName}.json",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string path = saveFileDialog.FileName;
                    SaveShowToPath(path);
                    _currentShowPath = path;      
                    MessageBox.Show($"Show saved to '{path}'",
                        "Save As", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============================================================
        // SAVE
        // ============================================================
        [RelayCommand]
        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentShowPath))
                {
                    SaveAs();
                    return;
                }

                SaveShowToPath(_currentShowPath);
                MessageBox.Show($"Show saved to '{_currentShowPath}'",
                    "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenShow()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Open Existing Show",
                    Filter = "JSON files (*.json)|*.json",
                    Multiselect = false,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedPath = openFileDialog.FileName;
                    string jsonString = File.ReadAllText(selectedPath);

                    var doc = JsonDocument.Parse(jsonString);
                    if (!doc.RootElement.TryGetProperty("show", out var showElement))
                    {
                        Message = "Het geselecteerde bestand bevat geen geldige 'show'-structuur.";
                        return;
                    }

                    var loadedShow = JsonSerializer.Deserialize<Shows>(showElement.GetRawText());
                    if (loadedShow == null)
                    {
                        Message = "Kon show niet deserialiseren. Bestand mogelijk corrupt.";
                        return;
                    }

                    _show = loadedShow;

                    currentShowId = _show.Id;
                    CurrentShowName = _show.Name;
                    _currentShowPath = selectedPath;

                    Scenes.Clear();
                    if (_show.Scenes != null)
                    {
                        foreach (var scene in _show.Scenes)
                            Scenes.Add(scene);
                    }

                    Message = $"Show '{_show.Name}' succesvol geopend!";
                }
            }
            catch (JsonException)
            {
                Message = "Het geselecteerde bestand bevat ongeldige JSON.";
            }
            catch (Exception ex)
            {
                Message = $"Er is een fout opgetreden bij het openen van de show:\n{ex.Message}";
            }
        }

        private void SaveShowToPath(string path)
        {
            // Zorg dat _show up-to-date is
            _show.Id = currentShowId ?? GenerateRandomId();
            _show.Name = CurrentShowName ?? "Unnamed Show";
            _show.Scenes = Scenes.ToList();

            // Wrap in "show" object for compatible JSON
            var wrapper = new { show = _show };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(wrapper, options);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private string GenerateRandomId()
        {
            Random rnd = new Random();
            int number = rnd.Next(1, 999);
            string id = number.ToString();
            return id;
        }
    }
}