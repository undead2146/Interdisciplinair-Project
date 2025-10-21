using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Show.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace InterdisciplinairProject.ViewModels
{
    public partial class ScenebuilderViewModel : ObservableObject
    {
        // Read scenes from %LocalAppData%\InterdisciplinairProject\scenes.json
        private string GetScenesFilePath()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InterdisciplinairProject", "scenes.json");

        public ObservableCollection<Scene> Scenes { get; } = new();

        public ScenebuilderViewModel()
        {
            LoadScenes();
        }

        [RelayCommand]
        private void NewScene()
        {
            try
            {
                var dlg = new InterdisciplinairProject.Views.InputDialog("Nieuwe scène", "Geef een naam voor de scène:");
                if (dlg.ShowDialog() == true)
                {
                    var name = dlg.InputText?.Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        MessageBox.Show("Naam mag niet leeg zijn.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var scene = new Scene
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = name,
                        // keep dimmer default (0) — not required
                        Dimmer = 0,
                        // keep fixtures present but empty
                        Fixtures = new System.Collections.Generic.List<Fixture>()
                    };

                    Scenes.Add(scene);
                    SaveScenes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadScenes()
        {
            try
            {
                var file = GetScenesFilePath();
                if (!File.Exists(file))
                {
                    // nothing to load
                    return;
                }

                var json = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Support: single file containing { "scene": { ... } }
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("scene", out var sceneElement))
                {
                    var scene = JsonSerializer.Deserialize<Scene>(sceneElement.GetRawText(), options);
                    if (scene != null && !Scenes.Any(s => s.Id == scene.Id))
                        Scenes.Add(scene);
                    return;
                }

                // Support: root is an array (items can be direct Scene objects OR wrappers with "scene")
                if (root.ValueKind == JsonValueKind.Array)
                {
                    var list = new System.Collections.Generic.List<Scene>();
                    foreach (var item in root.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("scene", out var inner))
                        {
                            var s = JsonSerializer.Deserialize<Scene>(inner.GetRawText(), options);
                            if (s != null && !list.Any(x => x.Id == s.Id))
                                list.Add(s);
                        }
                        else
                        {
                            var s = JsonSerializer.Deserialize<Scene>(item.GetRawText(), options);
                            if (s != null && !list.Any(x => x.Id == s.Id))
                                list.Add(s);
                        }
                    }

                    Scenes.Clear();
                    foreach (var s in list) Scenes.Add(s);
                    return;
                }

                // Support: object with 'scenes' property containing array
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("scenes", out var scenesElement) && scenesElement.ValueKind == JsonValueKind.Array)
                {
                    var list = JsonSerializer.Deserialize<System.Collections.Generic.List<Scene>>(scenesElement.GetRawText(), options);
                    if (list != null)
                    {
                        Scenes.Clear();
                        foreach (var s in list)
                            Scenes.Add(s);
                    }
                    return;
                }

                // Fallback: try to deserialize root as a Scene directly
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var single = JsonSerializer.Deserialize<Scene>(json, options);
                    if (single != null && !Scenes.Any(s => s.Id == single.Id))
                        Scenes.Add(single);
                }
            }
            catch (JsonException jex)
            {
                MessageBox.Show($"Invalid JSON in scenes file: {jex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading scenes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveScenes()
        {
            try
            {
                var dir = Path.GetDirectoryName(GetScenesFilePath());
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                // We will store scenes in the requested wrapper format:
                // [ { "scene": { "id": "...", "name": "...", "universe": null, "fixtures": [ ... ] } }, ... ]
                var wrappers = Scenes.Select(s => new SceneWrapper
                {
                    scene = new SceneData
                    {
                        id = s.Id,
                        name = s.Name,
                        universe = null, // keep universe present but empty (null)
                        fixtures = s.Fixtures ?? new System.Collections.Generic.List<Fixture>()
                    }
                }).ToList();

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    // include nulls so "universe": null is written
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };

                var json = JsonSerializer.Serialize(wrappers, options);
                File.WriteAllText(GetScenesFilePath(), json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving scenes.json: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper DTOs to produce the exact requested JSON shape
        private class SceneWrapper
        {
            [JsonPropertyName("scene")]
            public SceneData? scene { get; set; }
        }

        private class SceneData
        {
            // match the requested property names
            [JsonPropertyName("id")]
            public string? id { get; set; }

            [JsonPropertyName("name")]
            public string? name { get; set; }

            [JsonPropertyName("universe")]
            public int? universe { get; set; }

            [JsonPropertyName("fixtures")]
            public System.Collections.Generic.List<Fixture>? fixtures { get; set; }
        }
    }
}