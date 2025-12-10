using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Services;
using InterdisciplinairProject.Fixtures.ViewModels;
using InterdisciplinairProject.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using SceneModel = InterdisciplinairProject.Core.Models.Scene;

namespace InterdisciplinairProject.ViewModels.SceneEditor;

/// <summary>
/// ViewModel for editing a scene.
/// </summary>
public partial class SceneEditorViewModel : ObservableObject
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureRegistry _fixtureRegistry;
    private readonly IHardwareConnection _hardwareConnection;
    private readonly IDmxAddressValidator _dmxAddressValidator;

    [ObservableProperty]
    private SceneModel _scene = new();

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
    /// <param name="fixtureRegistry">The fixture registry.</param>
    /// <param name="hardwareConnection">The hardware connection.</param>
    public SceneEditorViewModel(ISceneRepository sceneRepository, IFixtureRepository fixtureRepository, IFixtureRegistry fixtureRegistry, IHardwareConnection hardwareConnection)
        : this(sceneRepository, fixtureRepository, fixtureRegistry, hardwareConnection, new DmxAddressValidator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneEditorViewModel"/> class.
    /// </summary>
    /// <param name="sceneRepository">The scene repository.</param>
    /// <param name="fixtureRepository">The fixture repository.</param>
    /// <param name="fixtureRegistry">The fixture registry.</param>
    /// <param name="hardwareConnection">The hardware connection.</param>
    /// <param name="dmxAddressValidator">The DMX address validator.</param>
    public SceneEditorViewModel(ISceneRepository sceneRepository, IFixtureRepository fixtureRepository, IFixtureRegistry fixtureRegistry, IHardwareConnection hardwareConnection, IDmxAddressValidator dmxAddressValidator)
    {
        _sceneRepository = sceneRepository;
        _fixtureRepository = fixtureRepository;
        _fixtureRegistry = fixtureRegistry;
        _hardwareConnection = hardwareConnection;
        _dmxAddressValidator = dmxAddressValidator;
    }

    /// <summary>
    /// Event raised when the scene has been updated.
    /// </summary>
    public event EventHandler<SceneModel>? SceneUpdated;

    /// <summary>
    /// Gets or sets the action to request showing the Fixture Registry.
    /// The ScenebuilderViewModel will set this callback.
    /// </summary>
    public Action<Action<Fixture>>? OnRequestFixtureRegistry { get; set; }

    /// <summary>
    /// Loads a scene for editing.
    /// </summary>
    /// <param name="scene">The scene to load.</param>
    public void LoadScene(SceneModel scene)
    {
        if (scene == null)
        {
            return;
        }

        Scene = scene;
        SceneFixtures.Clear();

        if (scene.Fixtures != null)
        {
            foreach (var fixture in scene.Fixtures)
            {
                // Als de fixture nog geen StartAddress heeft, bereken deze dan
                if (fixture.StartAddress == 0 || fixture.StartAddress == 1)
                {
                    fixture.StartAddress = GetNextAvailableChannel();
                }

                SceneFixtures.Add(new SceneFixture { Fixture = fixture, StartChannel = fixture.StartAddress });
            }
        }
    }

    /// <summary>
    /// Automatically saves the scene after changes.
    /// </summary>
    private async Task AutoSaveScene()
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

            Debug.WriteLine($"[DEBUG] Auto-saving scene '{Scene.Name}' with {Scene.Fixtures?.Count ?? 0} fixtures");

            // Sla de complete scene op via repository
            await _sceneRepository.SaveSceneAsync(Scene);

            Debug.WriteLine($"[DEBUG] Scene '{Scene.Name}' auto-saved successfully");

            // Raise event om parent (ScenebuilderViewModel) te notificeren
            SceneUpdated?.Invoke(this, Scene);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error auto-saving scene: {ex.Message}");
            MessageBox.Show($"Fout bij automatisch opslaan van scene: {ex.Message}", "Waarschuwing", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Edits the scene name.
    /// </summary>
    [RelayCommand]
    private async Task EditSceneName()
    {
        try
        {
            var dialog = new SceneNameDialog(
                "Scène naam bewerken",
                "Geef een nieuwe naam voor de scène:",
                Scene.Name ?? string.Empty);

            if (dialog.ShowDialog() == true)
            {
                var newName = dialog.InputText?.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != Scene.Name)
                {
                    Scene.Name = newName;
                    Debug.WriteLine($"[DEBUG] Scene name changed to '{newName}'");

                    // Trigger property change notification for UI update
                    OnPropertyChanged(nameof(Scene));

                    // Automatically save the scene
                    await _sceneRepository.SaveSceneAsync(Scene);
                    Debug.WriteLine($"[DEBUG] Scene '{newName}' automatically saved");

                    // Raise event to notify parent (ScenebuilderViewModel) with the updated scene
                    SceneUpdated?.Invoke(this, Scene);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error editing scene name: {ex.Message}");
            MessageBox.Show($"Error editing scene name: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Previews the scene by sending all fixture values to the DMX controller.
    /// </summary>
    [RelayCommand]
    private async Task PreviewScene()
    {
        try
        {
            Debug.WriteLine($"[DEBUG] PreviewScene button clicked for scene '{Scene.Name}'");

            if (Scene.Fixtures == null || Scene.Fixtures.Count == 0)
            {
                MessageBox.Show("De scene bevat geen fixtures om te testen.", "Geen Fixtures", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Debug.WriteLine($"[DEBUG] Sending scene with {Scene.Fixtures.Count} fixtures to DMX controller");

            bool success = await _hardwareConnection.SendSceneAsync(Scene);

            if (success)
            {
                Debug.WriteLine("[DEBUG] Scene preview sent successfully");
                MessageBox.Show($"Scene '{Scene.Name}' succesvol naar DMX controller gestuurd!", "Preview Gelukt", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Debug.WriteLine("[DEBUG] Scene preview failed");
                MessageBox.Show("Fout bij het versturen van de scene naar de DMX controller. Controleer of een COM poort beschikbaar is.", "Preview Mislukt", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error previewing scene: {ex.Message}");
            MessageBox.Show($"Error bij het versturen van de scene: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Adds a new fixture to the scene.
    /// Shows the Fixture Registry List in the left panel.
    /// </summary>
    [RelayCommand]
    private void AddFixture()
    {
        try
        {
            Debug.WriteLine("[DEBUG] AddFixture button clicked - requesting Fixture Registry");

            // Call the callback to show Fixture Registry
            // Pass a callback that will be called when a fixture is selected
            OnRequestFixtureRegistry?.Invoke(async (selectedFixture) =>
            {
                await HandleFixtureSelected(selectedFixture);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error requesting fixture registry: {ex.Message}");
            MessageBox.Show($"Error opening fixture registry: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles when a fixture is selected from the Fixture Registry.
    /// </summary>
    private async Task HandleFixtureSelected(Fixture selectedFixture)
    {
        try
        {
            Debug.WriteLine($"[DEBUG] Fixture selected from registry: {selectedFixture.Name}");

            // Validate the DMX address
            var existingFixtures = SceneFixtures.Select(sf => sf.Fixture).ToList();
            var validationResult = _dmxAddressValidator.ValidateFixtureAddress(selectedFixture, existingFixtures);

            if (!validationResult.IsValid)
            {
                // There are conflicts
                var message = $"DMX adres conflict gedetecteerd!\n\n{validationResult.Summary}";

                if (validationResult.SuggestedStartAddress.HasValue)
                {
                    message += $"\n\nSuggestie: Gebruik startadres {validationResult.SuggestedStartAddress.Value}.";

                    var result = MessageBox.Show(
                        message + "\n\nWilt u het gesuggereerde adres gebruiken?",
                        "Adres Conflict",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Use the suggested address
                        selectedFixture.StartAddress = validationResult.SuggestedStartAddress.Value;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        // Cancel adding
                        Debug.WriteLine($"[DEBUG] User cancelled adding fixture due to address conflict");
                        return;
                    }

                    // If No: add with conflicting address (user knows what they're doing)
                }
                else
                {
                    MessageBox.Show(
                        message + "\n\nEr is geen ruimte meer beschikbaar in het DMX universum.",
                        "Adres Conflict",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }

            // Create a new instance of the fixture for this scene
            var newFixture = new Fixture
            {
                FixtureId = selectedFixture.FixtureId,
                InstanceId = Guid.NewGuid().ToString(), // New unique instance ID
                Name = selectedFixture.Name,
                Manufacturer = selectedFixture.Manufacturer,
                Description = selectedFixture.Description,
                Channels = new ObservableCollection<Channel>(selectedFixture.Channels.Select(ch => new Channel
                {
                    Name = ch.Name,
                    Type = ch.Type,
                    Value = ch.Value,
                    Parameter = ch.Parameter,
                    Min = ch.Min,
                    Max = ch.Max,
                    Time = ch.Time,
                    ChannelEffect = ch.ChannelEffect,
                })),
                ChannelDescriptions = new Dictionary<string, string>(selectedFixture.ChannelDescriptions),
                StartAddress = selectedFixture.StartAddress,
                ImageBase64 = selectedFixture.ImageBase64,
            };

            // Add to the scene
            SceneFixtures.Add(new SceneFixture
            {
                Fixture = newFixture,
                StartChannel = newFixture.StartAddress,
            });

            Debug.WriteLine($"[DEBUG] Fixture '{newFixture.Name}' added to scene at DMX address {newFixture.StartAddress}");

            // Save the scene to persist the new fixture
            await AutoSaveScene();
            Debug.WriteLine($"[DEBUG] Scene auto-saved after adding fixture '{newFixture.Name}'");

            MessageBox.Show(
                $"Fixture '{newFixture.Name}' succesvol toegevoegd aan scene!",
                "Succes",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error handling selected fixture: {ex.Message}");
            MessageBox.Show($"Fout bij toevoegen van fixture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler om de geselecteerde fixture toe te voegen.
    /// </summary>
    private async void FixtureListViewModel_FixtureSelected(object? sender, string json)
    {
        // Deabonneer van het event
        if (sender is FixtureListViewModel vm)
        {
            vm.FixtureSelected -= FixtureListViewModel_FixtureSelected;
        }

        try
        {
            // Deserialiseer de JSON naar het Core.Models.Fixture type
            var tempFixture = JsonSerializer.Deserialize<InterdisciplinairProject.Core.Models.Fixture>(json);

            if (tempFixture != null)
            {
                // Converteer de ObservableCollection<Fixtures.Models.Channel> naar de
                // ObservableCollection<Channel> die Core.Models.Fixture verwacht.
                var channels = new ObservableCollection<InterdisciplinairProject.Core.Models.Channel>();
                var descriptionDictionary = new Dictionary<string, string>();

                int channelIndex = 1;
                foreach (var channel in tempFixture.Channels)
                {
                    string channelKey = $"Ch{channelIndex}";

                    // Voeg toe aan de channels collection
                    channels.Add(new InterdisciplinairProject.Core.Models.Channel
                    {
                        Name = channelKey,
                        Type = channel.Type,
                        Value = "0",
                        Parameter = 0,
                        Min = channel.Min,
                        Max = channel.Max,
                        Time = channel.Time,
                        ChannelEffect = channel.ChannelEffect,
                    });

                    // Creëer de beschrijving
                    string description = $"{channelKey}: {channel.Type} - {channel.Name}";
                    descriptionDictionary.Add(channelKey, description);

                    channelIndex++;
                }

                // Bereken het volgende beschikbare DMX adres
                var nextAvailableAddress = GetNextAvailableChannel();

                // Maak de Core Fixture aan
                var newCoreFixture = new InterdisciplinairProject.Core.Models.Fixture
                {
                    Name = tempFixture.Name,
                    Manufacturer = tempFixture.Manufacturer,
                    Channels = channels,
                    ChannelDescriptions = descriptionDictionary,
                    InstanceId = Guid.NewGuid().ToString(),
                    FixtureId = tempFixture.Name,
                    StartAddress = nextAvailableAddress,
                };

                // Valideer het DMX adres voordat de fixture wordt toegevoegd
                var existingFixtures = SceneFixtures.Select(sf => sf.Fixture).ToList();
                var validationResult = _dmxAddressValidator.ValidateFixtureAddress(newCoreFixture, existingFixtures);

                if (!validationResult.IsValid)
                {
                    var message = $"DMX adres conflict gedetecteerd!\n\n{validationResult.Summary}";

                    if (validationResult.SuggestedStartAddress.HasValue)
                    {
                        message += $"\n\nSuggestie: Gebruik startadres {validationResult.SuggestedStartAddress.Value}.";

                        var result = MessageBox.Show(
                            message + "\n\nWilt u het gesuggereerde adres gebruiken?",
                            "Adres Conflict",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            newCoreFixture.StartAddress = validationResult.SuggestedStartAddress.Value;
                        }
                        else if (result == MessageBoxResult.Cancel)
                        {
                            Debug.WriteLine($"[DEBUG] User cancelled adding fixture due to address conflict");
                            CurrentView = null; // Sluit FixtureListView
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            message + "\n\nEr is geen ruimte meer beschikbaar in het DMX universum.",
                            "Adres Conflict",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        CurrentView = null; // Sluit FixtureListView
                        return;
                    }
                }

                // Voeg de nieuwe fixture toe aan de scene
                var sceneFixture = new SceneFixture { Fixture = newCoreFixture, StartChannel = newCoreFixture.StartAddress };

                SceneFixtures.Add(sceneFixture);

                // ✅ AUTOMATISCH OPSLAAN NA TOEVOEGEN
                await AutoSaveScene();

                // Selecteer de nieuwe fixture zodat de settings view wordt getoond
                SelectedFixture = sceneFixture;

                Debug.WriteLine($"[DEBUG] Added and saved fixture '{newCoreFixture.Name}' to scene at channel {newCoreFixture.StartAddress}");
                MessageBox.Show($"Fixture '{newCoreFixture.Name}' succesvol toegevoegd!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error processing selected fixture: {ex.Message}");
            MessageBox.Show($"Fout bij het verwerken van fixture-data: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            CurrentView = null;
        }
    }

    /// <summary>
    /// Refreshes the scene data from the repository.
    /// </summary>
    private async Task RefreshSceneFromRepository()
    {
        try
        {
            if (string.IsNullOrEmpty(Scene.Id))
            {
                Debug.WriteLine($"[WARNING] SceneEditorViewModel: Scene has no ID, cannot refresh from repository");
                return;
            }

            // Reload the scene from repository to get the updated version
            var updatedScene = await _sceneRepository.GetSceneByIdAsync(Scene.Id);

            if (updatedScene != null)
            {
                LoadScene(updatedScene);
                Debug.WriteLine($"[DEBUG] SceneEditorViewModel: Refreshed scene '{updatedScene.Name}' with {updatedScene.Fixtures?.Count ?? 0} fixtures from repository");
            }
            else
            {
                Debug.WriteLine($"[WARNING] SceneEditorViewModel: Could not reload scene '{Scene.Id}' from repository");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] SceneEditorViewModel: Error refreshing scene from repository: {ex.Message}");
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
            var endChannel = sf.Fixture.StartAddress + channelCount - 1;
            if (endChannel > maxChannel)
            {
                maxChannel = endChannel;
            }
        }

        return maxChannel + 1;
    }

    /// <summary>
    /// Event handler for when a fixture is selected from the registry list.
    /// </summary>
    private async void OnFixtureSelectedFromRegistry(object? sender, InterdisciplinairProject.Core.Models.Fixture fixture)
    {
        // Close the list view
        CurrentView = null;

        /*
        if (sender is FixtureRegistryImportViewModel vm)
        {
            vm.FixtureSelected -= OnFixtureSelectedFromRegistry;
        }
        */

        try
        {
            // Valideer het DMX adres voordat de fixture wordt toegevoegd
            var existingFixtures = SceneFixtures.Select(sf => sf.Fixture).ToList();
            var validationResult = _dmxAddressValidator.ValidateFixtureAddress(fixture, existingFixtures);

            if (!validationResult.IsValid)
            {
                // Er zijn conflicten gevonden
                var message = $"DMX adres conflict gedetecteerd!\n\n{validationResult.Summary}";

                if (validationResult.SuggestedStartAddress.HasValue)
                {
                    message += $"\n\nSuggestie: Gebruik startadres {validationResult.SuggestedStartAddress.Value}.";

                    var result = MessageBox.Show(
                        message + "\n\nWilt u het gesuggereerde adres gebruiken?",
                        "Adres Conflict",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Gebruik het gesuggereerde adres
                        fixture.StartAddress = validationResult.SuggestedStartAddress.Value;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        // Annuleer het toevoegen
                        Debug.WriteLine($"[DEBUG] User cancelled adding fixture due to address conflict");
                        return;
                    }

                    // Bij No: voeg toe met het conflicterende adres (gebruiker weet wat hij doet)
                }
                else
                {
                    MessageBox.Show(
                        message + "\n\nEr is geen ruimte meer beschikbaar in het DMX universum.",
                        "Adres Conflict",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }

            // Voeg de nieuwe fixture toe aan de scene
            var sceneFixture = new SceneFixture { Fixture = fixture, StartChannel = fixture.StartAddress };

            SceneFixtures.Add(sceneFixture);

            // ✅ AUTOMATISCH OPSLAAN NA TOEVOEGEN
            await AutoSaveScene();

            // Selecteer de nieuwe fixture zodat de settings view wordt getoond
            SelectedFixture = sceneFixture;

            Debug.WriteLine($"[DEBUG] Added fixture '{fixture.Name}' from registry to scene at channel {fixture.StartAddress}");
            MessageBox.Show($"Fixture '{fixture.Name}' succesvol toegevoegd!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error adding fixture from registry: {ex.Message}");
            MessageBox.Show($"Fout bij het toevoegen van fixture: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

            // ✅ AUTOMATISCH OPSLAAN NA VERWIJDEREN
            await AutoSaveScene();

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