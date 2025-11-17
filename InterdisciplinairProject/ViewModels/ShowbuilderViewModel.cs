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
using InterdisciplinairProject.Views;
using System.Diagnostics;

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
        // EVENT: OnSceneChanged
        // ============================================================
        /// <summary>
        /// Event that is triggered when a scene is modified, added, or deleted.
        /// </summary>
        public event EventHandler<SceneChangedEventArgs>? OnSceneChanged;

        /// <summary>
        /// Raises the OnSceneChanged event.
        /// </summary>
        /// <param name="scene">The scene that was changed.</param>
        /// <param name="changeType">The type of change that occurred.</param>
        protected virtual void RaiseSceneChanged(Scene? scene, SceneChangeType changeType)
        {
            OnSceneChanged?.Invoke(this, new SceneChangedEventArgs(scene, changeType));
            // Added console logging
            Debug.WriteLine($"[SceneChanged] Scene: '{scene?.Name}', Type: {changeType}");
        }

        // ============================================================
        // EXPORT SCENE (was SaveSceneParameters)
        // ============================================================
        /// <summary>
        /// Exports a specific scene to a JSON file with the correct wrapper format.
        /// This allows the scene to be imported later via ImportScenes.
        /// </summary>
        [RelayCommand]
        private void ExportScene(Scene? scene)
        {
            if (scene == null)
            {
                MessageBox.Show("Geen scene geselecteerd om te exporteren.", "Fout", MessageBoxButton.OK, MessageBoxImage.Warning);
                Debug.WriteLine("[ExportScene] Geen scene geselecteerd om te exporteren.");
                return;
            }

            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Scene Exporteren",
                    Filter = "JSON files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"{scene.Name}.json",
                    AddExtension = true
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Wrap scene in "scene" object for compatibility with SceneExtractor
                    var wrapper = new { scene = scene };
                    
                    var options = new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        PropertyNamingPolicy = null, // Don't convert property names to camelCase
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                    };
                    string json = JsonSerializer.Serialize(wrapper, options);
                    File.WriteAllText(saveFileDialog.FileName, json, Encoding.UTF8);

                    Message = $"Scene '{scene.Name}' geëxporteerd naar '{saveFileDialog.FileName}'";
                    MessageBox.Show($"Scene succesvol geëxporteerd!\nDimmer waarde: {scene.Dimmer}", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                    Debug.WriteLine($"[ExportScene] Scene '{scene.Name}' exported to '{saveFileDialog.FileName}'");

                    RaiseSceneChanged(scene, SceneChangeType.Exported);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij exporteren scene: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[ExportScene][Error] {ex}");
            }
        }

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
                Debug.WriteLine($"[CreateShow] Nieuwe show '{vm.ShowName}' aangemaakt.");
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
                        // ensure imported scene slider starts at 0
                        scene.Dimmer = 0;
                        Scenes.Add(scene);
                        Message = $"Scene '{scene.Name}' geïmporteerd!";
                        Debug.WriteLine($"[ImportScenes] Scene '{scene.Name}' imported from '{selectedScenePath}'");

                        RaiseSceneChanged(scene, SceneChangeType.Added);
                    }
                    else
                    {
                        Message = "Deze scene is al geïmporteerd.";
                        MessageBox.Show("Deze scene bestaat al in de huidige show.", "Scene Bestaat Al", MessageBoxButton.OK, MessageBoxImage.Information);
                        Debug.WriteLine($"[ImportScenes] Scene '{scene.Name}' already exists in the current show.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[ImportScenes][Error] {ex}");
            }
        }

        // ============================================================
        // SCENE SELECTION
        // ============================================================
        [RelayCommand]
        private void SceneSelectionChanged(Scene selectedScene)
        {
            SelectedScene = selectedScene;
            RaiseSceneChanged(selectedScene, SceneChangeType.Selected);
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
                    Debug.WriteLine($"[SaveAs] Show saved to '{path}'");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[SaveAs][Error] {ex}");
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
                Debug.WriteLine($"[Save] Show saved to '{_currentShowPath}'");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"[Save][Error] {ex}");
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
                        Debug.WriteLine("[OpenShow] Invalid show structure in selected file.");
                        return;
                    }

                    var loadedShow = JsonSerializer.Deserialize<Shows>(showElement.GetRawText());
                    if (loadedShow == null)
                    {
                        Message = "Kon show niet deserialiseren. Bestand mogelijk corrupt.";
                        Debug.WriteLine("[OpenShow] Could not deserialize show. Possibly corrupted file.");
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
                        {
                            // when opening/importing a show, reset dimmer to 0 so sliders start off
                            scene.Dimmer = 0;
                            Scenes.Add(scene);
                        }
                    }

                    Message = $"Show '{_show.Name}' succesvol geopend!";
                    Debug.WriteLine($"[OpenShow] Show '{_show.Name}' opened from '{selectedPath}'");
                }
            }
            catch (JsonException)
            {
                Message = "Het geselecteerde bestand bevat ongeldige JSON.";
                Debug.WriteLine("[OpenShow] Invalid JSON file.");
            }
            catch (Exception ex)
            {
                Message = $"Er is een fout opgetreden bij het openen van de show:\n{ex.Message}";
                Debug.WriteLine($"[OpenShow][Error] {ex}");
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
            Debug.WriteLine($"[SaveShowToPath] Show '{_show.Name}' written to '{path}'");
        }

        private string GenerateRandomId()
        {
            Random rnd = new Random();
            int number = rnd.Next(1, 999);
            string id = number.ToString();
            return id;
        }

        // ============================================================
        // DELETE SCENE
        // ============================================================
        [RelayCommand]
        private void DeleteScene(Scene? scene)
        {
            if (scene == null)
                return;

            // Ask for confirmation before deleting
            var result = MessageBox.Show(
                $"Weet je zeker dat je de scene '{scene.Name}' wilt verwijderen?",
                "Bevestig verwijderen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // remove from the UI collection
            if (Scenes.Contains(scene))
                Scenes.Remove(scene);

            // keep underlying show in sync if needed
            if (_show?.Scenes != null && _show.Scenes.Contains(scene))
                _show.Scenes.Remove(scene);

            Message = $"Scene '{scene.Name}' verwijderd.";
            Debug.WriteLine($"[DeleteScene] Scene '{scene.Name}' deleted.");
            RaiseSceneChanged(scene, SceneChangeType.Deleted);
        }
    }

    // ============================================================
    // SCENE CHANGE EVENT ARGS
    // ============================================================
    /// <summary>
    /// Event arguments for scene change events.
    /// </summary>
    public class SceneChangedEventArgs : EventArgs
    {
        public Scene? Scene { get; }
        public SceneChangeType ChangeType { get; }

        public SceneChangedEventArgs(Scene? scene, SceneChangeType changeType)
        {
            Scene = scene;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// Enum representing the type of scene change.
    /// </summary>
    public enum SceneChangeType
    {
        Added,
        Deleted,
        Modified,
        Selected,
        Exported
    }
}