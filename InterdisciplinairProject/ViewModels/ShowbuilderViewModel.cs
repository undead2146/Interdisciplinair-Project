using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Show;
using Show.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using static System.Formats.Asn1.AsnWriter;

namespace InterdisciplinairProject.ViewModels
{
    public partial class ShowbuilderViewModel : ObservableObject
    {
        public Shows _show = new Shows();

        public ObservableCollection<Scene> Scenes { get; } = new();

        private string? _currentShowPath;

        [ObservableProperty]
        private string? currentShowName;

        [RelayCommand]
        private void ImportShow() 
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string jsonFilePath = System.IO.Path.Combine(path, "Show.json");
            string jsonString = File.ReadAllText(jsonFilePath);

            var doc = JsonDocument.Parse(jsonString);
            if (!doc.RootElement.TryGetProperty("show", out var showElement))
            {
                throw new InvalidDataException("The JSON file does not contain a 'scene' property.");
            }
            
            _show = JsonSerializer.Deserialize<Shows>(showElement.GetRawText());

            CurrentShowName = _show.Name;

            MessageBox.Show(_show.DisplayText);
        }
        
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
                    FileName = string.IsNullOrWhiteSpace(CurrentShowName) ? "NewShow.json" : $"{CurrentShowName}.json",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string path = saveFileDialog.FileName;
                    SaveShowToPath(path);
                    _currentShowPath = path;
                    MessageBox.Show($"Show saved to '{path}'", "Save As", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                MessageBox.Show($"Show saved to '{_currentShowPath}'", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveShowToPath(string path)
        {
            var showObject = new
            {
                name = CurrentShowName ?? string.Empty,
                scenes = Scenes.ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            string json = JsonSerializer.Serialize(showObject, options);
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
        }
    }
}
