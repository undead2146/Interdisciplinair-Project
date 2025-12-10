using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.ViewModels.SceneEditor;
using InterdisciplinairProject.Views;
using Show;
using System.Collections.ObjectModel;
using System.Windows;
using InterdisciplinairProject.ViewModels;
using InterdisciplinairProject.Views.Scene;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the Scene Builder functionality.
/// </summary>
public partial class ScenebuilderViewModel : ObservableObject
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureRegistry _fixtureRegistry;
    private readonly IHardwareConnection _hardwareConnection;

    [ObservableProperty]
    private InterdisciplinairProject.Core.Models.Scene? _selectedScene;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private object? _leftPanelView;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenebuilderViewModel"/> class.
    /// </summary>
    /// <param name="sceneRepository">The scene repository.</param>
    /// <param name="fixtureRepository">The fixture repository.</param>
    /// <param name="fixtureRegistry">The fixture registry.</param>
    /// <param name="hardwareConnection">The hardware connection.</param>
    /// 
    public ScenebuilderViewModel(
    ISceneRepository sceneRepository,
    IFixtureRepository fixtureRepository,
    IFixtureRegistry fixtureRegistry,
    IHardwareConnection hardwareConnection)
    {
        _sceneRepository = sceneRepository;
        _fixtureRepository = fixtureRepository;
        _fixtureRegistry = fixtureRegistry;
        _hardwareConnection = hardwareConnection;

        // Seed test fixtures

        // Toon standaard de Scene List View
        ShowSceneList();

        _ = LoadScenesAsync(); // fire-and-forget; constructor can't be async
    }

    /// <summary>
    /// Gets the collection of scenes.
    /// </summary>
    public ObservableCollection<InterdisciplinairProject.Core.Models.Scene> Scenes { get; } = new();

    /// <summary>
    /// Opens the scene editor for the selected scene.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task OpenSceneEditor()
    {
        if (SelectedScene == null || string.IsNullOrEmpty(SelectedScene.Id))
        {
            return;
        }

        try
        {
            var fullScene = await _sceneRepository.GetSceneByIdAsync(SelectedScene.Id);

            if (fullScene != null)
            {
                var sceneEditorViewModel = new SceneEditorViewModel(
                    _sceneRepository,
                    _fixtureRepository,
                    _fixtureRegistry,
                    _hardwareConnection);

                sceneEditorViewModel.LoadScene(fullScene);

                // Subscribe to SceneUpdated event to refresh the list
                sceneEditorViewModel.SceneUpdated += (s, updatedScene) =>
                {
                    RefreshSceneInList(updatedScene);
                };

                sceneEditorViewModel.OnRequestFixtureRegistry = ShowFixtureRegistry;

                // Toon SceneEditorView IN de SceneBuilder
                CurrentView = new SceneEditorView
                {
                    DataContext = sceneEditorViewModel,
                };
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading scene: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Creates a new scene.
    /// </summary>
    [RelayCommand]
    private async Task NewScene()
    {
        try
        {
            var dlg = new SceneNameDialog("Nieuwe scène", "Geef een naam voor de scène:");
            if (dlg.ShowDialog() == true)
            {
                var name = dlg.InputText?.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Naam mag niet leeg zijn.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var scene = new InterdisciplinairProject.Core.Models.Scene
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Dimmer = 0,
                    Fixtures = new System.Collections.Generic.List<Fixture>(),
                };

                // Gebruik repository om op te slaan
                await _sceneRepository.SaveSceneAsync(scene);

                Scenes.Add(scene);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Deletes a scene.
    /// </summary>
    /// <param name="scene">The scene to delete.</param>
    [RelayCommand]
    private async Task DeleteScene(InterdisciplinairProject.Core.Models.Scene scene)
    {
        try
        {
            if (scene == null || string.IsNullOrEmpty(scene.Id))
            {
                return;
            }

            var result = MessageBox.Show(
             $"Weet je zeker dat je scene '{scene.Name}' wilt verwijderen?",
             "Scene verwijderen",
             MessageBoxButton.YesNo,
             MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _sceneRepository.DeleteSceneAsync(scene.Id);
                Scenes.Remove(scene);

                // Als de verwijderde scene geselecteerd was, clear de view
                if (SelectedScene == scene)
                {
                    SelectedScene = null;
                    CurrentView = null;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij verwijderen van scene: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadScenesAsync()
    {
        try
        {
            var scenes = await _sceneRepository.GetAllScenesAsync();

            Scenes.Clear();
            foreach (var scene in scenes)
            {
                Scenes.Add(scene);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading scenes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Imports scenes from JSON files.
    /// </summary>
    [RelayCommand]
    private async Task ImportScenes()
    {
        try
        {
            // Open file dialog om JSON bestanden te selecteren
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Selecteer scene bestanden om te importeren",
                Filter = "JSON bestanden (*.json)|*.json|Alle bestanden (*.*)|*.*",
                Multiselect = true,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                int successCount = 0;
                int failCount = 0;
                var errorMessages = new System.Collections.Generic.List<string>();

                foreach (var filePath in openFileDialog.FileNames)
                {
                    try
                    {
                        // Gebruik de nieuwe import methode met validatie
                        var (importedScenes, error) = await _sceneRepository.ImportScenesFromFileAsync(filePath);

                        if (error != null)
                        {
                            failCount++;
                            errorMessages.Add($"{System.IO.Path.GetFileName(filePath)}: {error}");
                        }
                        else
                        {
                            // Voeg alle geïmporteerde scenes toe aan de lijst
                            foreach (var scene in importedScenes)
                            {
                                Scenes.Add(scene);
                                successCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errorMessages.Add($"{System.IO.Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                // Toon resultaat
                if (successCount > 0 && failCount == 0)
                {
                    MessageBox.Show(
                        $"{successCount} scene(s) succesvol geïmporteerd!",
                        "Import succesvol",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else if (successCount > 0 && failCount > 0)
                {
                    MessageBox.Show(
                        $"{successCount} scene(s) geïmporteerd, {failCount} bestand(en) gefaald.\n\nErrors:\n{string.Join("\n", errorMessages)}",
                        "Import gedeeltelijk succesvol",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                else if (failCount > 0)
                {
                    MessageBox.Show(
                        $"Alle imports gefaald.\n\nErrors:\n{string.Join("\n", errorMessages)}",
                        "Import gefaald",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij importeren van scenes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Exports all scenes to a JSON file.
    /// </summary>
    [RelayCommand]
    private async Task ExportScenes()
    {
        try
        {
            if (Scenes.Count == 0)
            {
                MessageBox.Show("Er zijn geen scenes om te exporteren.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open save file dialog
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exporteer scenes naar bestand",
                Filter = "JSON bestanden (*.json)|*.json|Alle bestanden (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"scenes_export_{DateTime.Now:yyyyMMdd_HHmmss}",
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var error = await _sceneRepository.ExportScenesToFileAsync(saveFileDialog.FileName);

                if (error != null)
                {
                    MessageBox.Show(error, "Export gefaald", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(
                        $"{Scenes.Count} scene(s) succesvol geëxporteerd naar:\n{saveFileDialog.FileName}",
                        "Export succesvol",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij exporteren van scenes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Deletes all scenes.
    /// </summary>
    [RelayCommand]
    private async Task DeleteAllScenes()
    {
        try
        {
            if (Scenes.Count == 0)
            {
                MessageBox.Show("Er zijn geen scenes om te verwijderen.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Weet je zeker dat je ALLE {Scenes.Count} scene(s) wilt verwijderen?\n\nDeze actie kan niet ongedaan gemaakt worden.",
                "Alle scenes verwijderen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _sceneRepository.DeleteAllScenesAsync();
                Scenes.Clear();
                SelectedScene = null;
                CurrentView = null;

                MessageBox.Show("Alle scenes zijn verwijderd.", "Verwijderd", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fout bij verwijderen van alle scenes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Refreshes a specific scene in the list after it has been updated.
    /// </summary>
    /// <param name="updatedScene">The updated scene.</param>
    private void RefreshSceneInList(InterdisciplinairProject.Core.Models.Scene updatedScene)
    {
        try
        {
            if (updatedScene != null && !string.IsNullOrEmpty(updatedScene.Id))
            {
                var existingScene = Scenes.FirstOrDefault(s => s.Id == updatedScene.Id);
                if (existingScene != null)
                {
                    var index = Scenes.IndexOf(existingScene);

                    // Remove and re-add to trigger ObservableCollection change notification
                    Scenes.RemoveAt(index);
                    Scenes.Insert(index, updatedScene);

                    // Update SelectedScene reference if it's the same scene
                    if (SelectedScene?.Id == updatedScene.Id)
                    {
                        SelectedScene = updatedScene;
                    }

                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Updated scene '{updatedScene.Name}' in list at index {index}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Error refreshing scene in list: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the Scene List in the left panel.
    /// </summary>
    private void ShowSceneList()
    {
        var sceneListView = new SceneListView
        {
            DataContext = this
        };
        LeftPanelView = sceneListView;
    }

    /// <summary>
    /// Shows the Fixture Registry List in the left panel.
    /// Called when the user clicks "Add Fixture" in the SceneEditorView.
    /// </summary>
    public void ShowFixtureRegistry(Action<Fixture> onFixtureSelected)
    {
        try
        {
            var fixtureRegistryViewModel = new FixtureRegistryListViewModel(_fixtureRegistry);

            // Subscribe to the FixtureSelected event
            fixtureRegistryViewModel.FixtureSelected += (sender, selectedFixture) =>
            {
                // Call the callback with the selected fixture
                onFixtureSelected?.Invoke(selectedFixture);

                // Switch back to Scene List
                ShowSceneList();
            };

            // Subscribe to the Cancel event
            fixtureRegistryViewModel.Cancel += () =>
            {
                // Switch back to Scene List
                ShowSceneList();
            };

            var fixtureRegistryView = new FixtureRegistryListView
            {
                DataContext = fixtureRegistryViewModel
            };

            LeftPanelView = fixtureRegistryView;

            System.Diagnostics.Debug.WriteLine("[DEBUG] Switched to Fixture Registry view");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Error showing fixture registry: {ex.Message}");
            MessageBox.Show($"Fout bij openen van fixture registry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}