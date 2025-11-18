using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using InterdisciplinairProject.Fixtures.Views;
using InterdisciplinairProject.Fixtures.ViewModels;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using InterdisciplinairProject.Fixtures.Models;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for editing a scene.
/// </summary>
public partial class SceneEditorViewModel : ObservableObject
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IHardwareConnection _hardwareConnection;

    [ObservableProperty]
    private Scene _scene = new();

    [ObservableProperty]
    private ObservableCollection<SceneFixture> _sceneFixtures = new();

    [ObservableProperty]
    private SceneFixture? _selectedFixture;

    [ObservableProperty]
    private object? _currentView;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneEditorViewModel"/> class.
    /// </summary>
    /// <param name="sceneRepository">The scene repository.</param>
    /// <param name="fixtureRepository">The fixture repository.</param>
    /// <param name="hardwareConnection">The hardware connection.</param>
    public SceneEditorViewModel(ISceneRepository sceneRepository, IFixtureRepository fixtureRepository, IHardwareConnection hardwareConnection)
    {
        _sceneRepository = sceneRepository;
        _fixtureRepository = fixtureRepository;
        _hardwareConnection = hardwareConnection;
    }

    /// <summary>
    /// Loads a scene for editing.
    /// </summary>
    /// <param name="scene">The scene to load.</param>
    public void LoadScene(Scene scene)
    {
        if (scene == null)
        {
            return;
        }

        Scene = scene;
        SceneFixtures.Clear();

        var currentChannel = 1;
        if (scene.Fixtures != null)
        {
            foreach (var fixture in scene.Fixtures)
            {
                SceneFixtures.Add(new SceneFixture { Fixture = fixture, StartChannel = currentChannel });
                currentChannel += fixture.Channels.Count;
            }
        }
    }

    /// <summary>
    /// Saves the scene.
    /// </summary>
    [RelayCommand]
    private async Task SaveScene()
    {
        try
        {
            // Synchroniseer de Scene.Fixtures met de SceneFixtures ListBox
            Scene.Fixtures ??= new List<InterdisciplinairProject.Core.Models.Fixture>();
            Scene.Fixtures.Clear();

            // Kopieer de huidige listbox inhoud naar het Core Scene object
            foreach (var sf in SceneFixtures)
            {
                Scene.Fixtures.Add(sf.Fixture);
            }

            Debug.WriteLine($"[DEBUG] Saving scene '{Scene.Name}' with {Scene.Fixtures?.Count ?? 0} fixtures");

            // Sla de complete scene op via repository
            await _sceneRepository.SaveSceneAsync(Scene);

            MessageBox.Show("Scene succesvol opgeslagen!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            Debug.WriteLine($"[DEBUG] Scene '{Scene.Name}' saved successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error saving scene: {ex.Message}");
            MessageBox.Show($"Error saving scene: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Adds a new fixture to the scene.
    /// </summary>
    [RelayCommand]
    private async Task AddFixture()
    {
        try
        {
            var fixtureListViewModel = new FixtureListViewModel();

            // Abonneren op het FixtureSelected event
            fixtureListViewModel.FixtureSelected += FixtureListViewModel_FixtureSelected;

            CurrentView = new FixtureListView
            {
                DataContext = fixtureListViewModel
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error opening fixture list view: {ex.Message}");
            MessageBox.Show($"Error opening fixture list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Event handler om de geselecteerde fixture toe te voegen
    private void FixtureListViewModel_FixtureSelected(object? sender, string json)
    {
        // Sluit de FixtureListView af
        CurrentView = null;

        if (sender is FixtureListViewModel vm)
        {
            vm.FixtureSelected -= FixtureListViewModel_FixtureSelected;
        }

        try
        {
            // Deserialiseer de JSON naar het Fixtures.Models.Fixture type
            var tempFixture = JsonSerializer.Deserialize<InterdisciplinairProject.Fixtures.Models.Fixture>(json);

            if (tempFixture != null)
            {
                // Converteer de ObservableCollection<Fixtures.Models.Channel> naar de
                // Dictionary<string, byte?> en Dictionary<string, string> die Core.Models.Fixture verwacht.

                var channelDictionary = new Dictionary<string, byte?>();
                var descriptionDictionary = new Dictionary<string, string>();

                int channelIndex = 1;
                foreach (var channel in tempFixture.Channels)
                {
                    string channelKey = $"Ch{channelIndex}";

                    // Voeg toe aan de channels dictionary (met default DMX waarde 0)
                    channelDictionary.Add(channelKey, 0);

                    // Creëer de beschrijving (bijv. "Ch1: Dimmer - General intensity")
                    string description = $"{channelKey}: {channel.Type} - {channel.Name}";
                    descriptionDictionary.Add(channelKey, description);

                    channelIndex++;
                }

                // Maak de Core Fixture aan
                var newCoreFixture = new InterdisciplinairProject.Core.Models.Fixture
                {
                    Name = tempFixture.Name,
                    Manufacturer = tempFixture.Manufacturer,

                    // Wijs de geconverteerde dictionaries toe
                    Channels = channelDictionary,
                    ChannelDescriptions = descriptionDictionary,

                    // Zorg ervoor dat Id uniek is voor de instance.
                    InstanceId = Guid.NewGuid().ToString(),
                    // De Id van de Fixture Type is hetzelfde als de Name in dit geval (aanname)
                    Id = tempFixture.Name
                };

                // Voeg de nieuwe fixture toe aan de scene
                var nextChannel = GetNextAvailableChannel();
                var sceneFixture = new SceneFixture { Fixture = newCoreFixture, StartChannel = nextChannel };

                SceneFixtures.Add(sceneFixture);
                SelectedFixture = sceneFixture;

                Debug.WriteLine($"[DEBUG] Added fixture '{newCoreFixture.Name}' to scene at channel {nextChannel}");
                MessageBox.Show($"Fixture '{newCoreFixture.Name}' succesvol toegevoegd aan de lijst!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error processing selected fixture: {ex.Message}");
            MessageBox.Show($"Fout bij het verwerken van fixture-data: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens the fixture settings view when a fixture is selected.
    /// </summary>
    partial void OnSelectedFixtureChanged(SceneFixture? value)
    {
        if (value?.Fixture != null)
        {
            // Maak een nieuwe FixtureSettingsViewModel
            var fixtureSettingsViewModel = new FixtureSettingsViewModel(_hardwareConnection);
            fixtureSettingsViewModel.LoadFixture(value.Fixture);

            // Laad de FixtureSettingsView
            CurrentView = new FixtureSettingsView
            {
                DataContext = fixtureSettingsViewModel
            };
        }
        else
        {
            CurrentView = null;
        }
    }

    private int GetNextAvailableChannel()
    {
        // Simple logic: find the highest end channel and add 1
        var maxChannel = 0;
        foreach (var sf in SceneFixtures)
        {
            // We controleren op null om crashes te voorkomen
            int channelCount = sf.Fixture.Channels?.Count ?? 0;
            var endChannel = sf.StartChannel + channelCount - 1;
            if (endChannel > maxChannel)
            {
                maxChannel = endChannel;
            }
        }

        return maxChannel + 1;
    }

    /// <summary>
    /// Removes a fixture from the scene.
    /// </summary>
    [RelayCommand]
    private async Task RemoveFixture(SceneFixture? fixtureToRemove)
    {
        if (fixtureToRemove == null)
        {
            Debug.WriteLine("[WARNING] RemoveFixtureCommand called without parameter.");
            return;
        }

        // Check if Scene.Id is valid
        if (string.IsNullOrEmpty(Scene.Id))
        {
            Debug.WriteLine("[ERROR] Scene has no ID, cannot remove fixture.");
            MessageBox.Show("Scene heeft geen geldig ID. Sla eerst de scene op.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var result = MessageBox.Show(
            $"Weet je zeker dat je de fixture '{fixtureToRemove.Fixture.Name}' wilt verwijderen?",
            "Bevestig Verwijdering",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.No)
        {
            return;
        }

        try
        {
            Debug.WriteLine($"[DEBUG] Removing fixture '{fixtureToRemove.Fixture.Name}' from scene '{Scene.Name}'.");

            SceneFixtures.Remove(fixtureToRemove);

            await _sceneRepository.RemoveFixtureAsync(Scene.Id, fixtureToRemove.Fixture);

            if (Scene.Fixtures != null)
            {
                Scene.Fixtures.Remove(fixtureToRemove.Fixture);
            }

            if (SelectedFixture == fixtureToRemove)
            {
                SelectedFixture = null;
            }

            Debug.WriteLine($"[DEBUG] Fixture successfully removed and scene saved.");

            MessageBox.Show(
                $"Fixture '{fixtureToRemove.Fixture.Name}' succesvol verwijderd.",
                "Succes",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error removing fixture: {ex.Message}");
            MessageBox.Show($"Fout bij verwijderen van fixture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}