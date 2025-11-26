using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Views; // 👈 Needed for CreateShowWindow
using InterdisciplinairProject.Core.Models;
using Show;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;

namespace InterdisciplinairProject.ViewModels
{
    public partial class ShowbuilderViewModel : ObservableObject
    {
        private InterdisciplinairProject.Core.Models.Show _show = new InterdisciplinairProject.Core.Models.Show();
        private string? _currentShowPath;

        public ObservableCollection<ShowScene> Scenes { get; } = new();

        [ObservableProperty]
        private ShowScene? selectedScene;

        [ObservableProperty]
        private string? currentShowId;

        [ObservableProperty]
        private string? currentShowName;

        [ObservableProperty]
        private string? message;

        // new: per-scene fade cancellation tokens
        private readonly Dictionary<ShowScene, CancellationTokenSource> _fadeCts = new();

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

                _show = new InterdisciplinairProject.Core.Models.Show
                {
                    Name = vm.ShowName,
                    Scenes = new List<ShowScene>()
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
                        // ensure imported scene slider starts at 0
                        var showScene = new ShowScene
                        {
                            Id = scene.Id,
                            Name = scene.Name,
                            Dimmer = 0,
                            FadeInMs = scene.FadeInMs,
                            FadeOutMs = scene.FadeOutMs,
                            Fixtures = scene.Fixtures?.Select(f => new Fixture
                            {
                                InstanceId = f.InstanceId,
                                FixtureId = f.FixtureId,
                                Name = f.Name,
                                Manufacturer = f.Manufacturer,
                                Dimmer = 0
                            }).ToList()
                        };
                        Scenes.Add(showScene);
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
        private void SceneSelectionChanged(ShowScene selectedScene)
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

                    var loadedShow = JsonSerializer.Deserialize<InterdisciplinairProject.Core.Models.Show>(showElement.GetRawText());
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
                        {
                            // when opening/importing a show, reset dimmer to 0 so sliders start off
                            scene.Dimmer = 0;
                            Scenes.Add(scene);
                        }
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

        // ============================================================
        // DELETE SCENE
        // ============================================================
        [RelayCommand]
        private void DeleteScene(ShowScene? scene)
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
        }

        public void UpdateSceneDimmer(ShowScene scene, int dimmer)
        {
            if (scene == null)
                return;

            // Cancel any fade in progress for this scene because user is manually changing it
            CancelFadeForScene(scene);

            dimmer = Math.Max(0, Math.Min(100, dimmer));

            // if we're turning this scene on (dimmer > 0), immediately turn all other scenes off.
            if (dimmer > 0)
            {
                foreach (var other in Scenes.ToList())
                {
                    if (!ReferenceEquals(other, scene) && other.Dimmer > 0)
                    {
                        other.Dimmer = 0;

                        // update other scene fixtures to 0
                        if (other.Fixtures != null)
                        {
                            foreach (var fixture in other.Fixtures)
                            {
                                try
                                {
                                    // set observable property if available
                                    fixture.Dimmer = 0;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"[DEBUG] Error zeroing fixture dimmer: {ex.Message}");
                                }
                            }
                        }

                        // refresh the Scenes collection item so UI updates if needed
                        var idx = Scenes.IndexOf(other);
                        if (idx >= 0) Scenes[idx] = other;
                    }
                }
            }

            // update model for the requested scene
            scene.Dimmer = dimmer;

            // update fixture channels for the requested scene
            if (scene.Fixtures != null)
            {
                byte channelValue = (byte)Math.Round(dimmer * 255.0 / 100.0);
                foreach (var fixture in scene.Fixtures)
                {
                    try
                    {
                        fixture.Dimmer = channelValue;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DEBUG] Error updating fixture channels/dimmer: {ex.Message}");
                    }
                }
            }

            // Ensure UI reflects changes
            if (SelectedScene == scene)
            {
                OnPropertyChanged(nameof(SelectedScene));
            }
            else
            {
                var idx = Scenes.IndexOf(scene);
                if (idx >= 0) Scenes[idx] = scene;
            }
        }

        // Cancels any running fade for the provided scene
        private void CancelFadeForScene(ShowScene scene)
        {
            if (scene == null) return;

            if (_fadeCts.TryGetValue(scene, out var cts))
            {
                try
                {
                    cts.Cancel();
                }
                catch { }
                cts.Dispose();
                _fadeCts.Remove(scene);
            }
        }

        // Fade a single scene to target over durationMs, updating fixtures and Scenes on the UI thread.
        private async Task FadeSceneAsync(ShowScene scene, int target, int durationMs, CancellationToken token)
        {
            if (scene == null) return;

            if (durationMs <= 0)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    scene.Dimmer = target;
                    UpdateFixturesForScene(scene, target);
                    var idx = Scenes.IndexOf(scene);
                    if (idx >= 0) Scenes[idx] = scene;
                    if (SelectedScene == scene) OnPropertyChanged(nameof(SelectedScene));
                });
                return;
            }

            const int intervalMs = 20;
            int steps = Math.Max(1, durationMs / intervalMs);

            // read authoritative start on UI thread (await returns the value)
            int start = await Application.Current.Dispatcher.InvokeAsync(() => scene.Dimmer);

            double delta = (target - start) / (double)steps;

            for (int i = 1; i <= steps; i++)
            {
                token.ThrowIfCancellationRequested();
                double next = start + delta * i;
                int nextInt = (int)Math.Round(Math.Max(0, Math.Min(100, next)));

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    scene.Dimmer = nextInt;
                    UpdateFixturesForScene(scene, nextInt);
                    var idx = Scenes.IndexOf(scene);
                    if (idx >= 0) Scenes[idx] = scene;
                    if (SelectedScene == scene) OnPropertyChanged(nameof(SelectedScene));
                });

                await Task.Delay(intervalMs, token);
            }

            // ensure exact target at end
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                scene.Dimmer = target;
                UpdateFixturesForScene(scene, target);
                var idx = Scenes.IndexOf(scene);
                if (idx >= 0) Scenes[idx] = scene;
                if (SelectedScene == scene) OnPropertyChanged(nameof(SelectedScene));
            });
        }

        // Helper to update fixture dimmer channels for a scene on the caller thread (call from UI dispatcher)
        private void UpdateFixturesForScene(ShowScene scene, int dimmer)
        {
            if (scene?.Fixtures == null) return;
            byte channelValue = (byte)Math.Round(dimmer * 255.0 / 100.0);
            foreach (var fixture in scene.Fixtures)
            {
                try
                {
                    fixture.Dimmer = channelValue;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DEBUG] Error updating fixture channels/dimmer: {ex.Message}");
                }
            }
        }

        // Public method used by SceneControlViewModel.PlayAsync to activate scene with fade orchestration.
        public async Task FadeToAndActivateAsync(ShowScene targetScene, int targetDimmer)
        {
            if (targetScene == null) return;

            // Cancel any fade for the target (we'll run a new one)
            CancelFadeForScene(targetScene);

            // collect currently active other scenes
            var activeOthers = Scenes.Where(s => !ReferenceEquals(s, targetScene) && s.Dimmer > 0).ToList();

            // fade out others in parallel
            var fadeOutTasks = new List<Task>();
            foreach (var other in activeOthers)
            {
                // cancel existing token for other and create a new one
                CancelFadeForScene(other);
                var cts = new CancellationTokenSource();
                _fadeCts[other] = cts;
                fadeOutTasks.Add(Task.Run(() => FadeSceneAsync(other, 0, Math.Max(0, other.FadeOutMs), cts.Token)));
            }

            try
            {
                // wait for all fade-outs to complete
                await Task.WhenAll(fadeOutTasks);
            }
            catch (OperationCanceledException) { /* one of fades cancelled; continue */ }

            // ensure other scenes are set to 0 (defensive)
            foreach (var other in activeOthers)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    other.Dimmer = 0;
                    UpdateFixturesForScene(other, 0);
                    var idx = Scenes.IndexOf(other);
                    if (idx >= 0) Scenes[idx] = other;
                });
                CancelFadeForScene(other);
            }

            // now fade target scene to requested dimmer using its FadeInMs
            CancelFadeForScene(targetScene);
            var ctsTarget = new CancellationTokenSource();
            _fadeCts[targetScene] = ctsTarget;

            try
            {
                await FadeSceneAsync(targetScene, targetDimmer, Math.Max(0, targetScene.FadeInMs), ctsTarget.Token);
            }
            finally
            {
                CancelFadeForScene(targetScene);
            }
        }
    }
}
