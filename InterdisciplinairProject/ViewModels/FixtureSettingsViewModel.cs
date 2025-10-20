using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Features.Fixture;
using InterdisciplinairProject.Services;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the fixture settings view.
/// </summary>
public class FixtureSettingsViewModel : INotifyPropertyChanged
{
    private readonly IHardwareConnection _hardwareConnection;
    private Fixture _currentFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureSettingsViewModel"/> class.
    /// </summary>
    public FixtureSettingsViewModel()
    {
        Debug.WriteLine("[DEBUG] FixtureSettingsViewModel constructor called");
        _hardwareConnection = new HardwareConnection();
        Debug.WriteLine("[DEBUG] HardwareConnection created");

        // Try to load from scenes.json in project root or AppData
        var scenesFilePath = FindScenesFile();
        Debug.WriteLine($"[DEBUG] Using scenes file: {scenesFilePath}");

        _currentFixture = LoadFirstFixtureFromScenes(scenesFilePath) ?? CreateDefaultFixture();

        Debug.WriteLine($"[DEBUG] Loaded fixture: {_currentFixture.Name} with {_currentFixture.Channels.Count} channels");

        // Create channel view models from the fixture's channels
        Channels = new ObservableCollection<ChannelViewModel>();
        LoadChannelsFromFixture(_currentFixture);
        Debug.WriteLine($"[DEBUG] FixtureSettingsViewModel initialization complete. Channels collection has {Channels.Count} items");
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the collection of channel view models.
    /// </summary>
    public ObservableCollection<ChannelViewModel> Channels { get; }

    /// <summary>
    /// Gets the name of the current fixture.
    /// </summary>
    public string FixtureName => _currentFixture?.Name ?? "No Fixture";

    /// <summary>
    /// Loads a new fixture into the view model.
    /// </summary>
    /// <param name="fixture">The fixture to load.</param>
    public void LoadFixture(Fixture fixture)
    {
        if (fixture == null)
        {
            throw new ArgumentNullException(nameof(fixture));
        }

        _currentFixture = fixture;
        LoadChannelsFromFixture(fixture);
        OnPropertyChanged(nameof(FixtureName));
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void LoadChannelsFromFixture(Fixture fixture)
    {
        Debug.WriteLine($"[DEBUG] LoadChannelsFromFixture called for fixture: {fixture.Name}");
        Channels.Clear();
        Debug.WriteLine("[DEBUG] Channels collection cleared");

        foreach (var channel in fixture.Channels)
        {
            var channelVm = new ChannelViewModel(channel.Key, channel.Value ?? 0);
            Debug.WriteLine($"[DEBUG] Created ChannelViewModel for channel: {channel.Key} = {channel.Value ?? 0}");

            // Subscribe to channel value changes
            channelVm.PropertyChanged += ChannelViewModel_PropertyChanged;
            Debug.WriteLine($"[DEBUG] Subscribed to PropertyChanged event for channel: {channel.Key}");

            Channels.Add(channelVm);
            Debug.WriteLine($"[DEBUG] Added ChannelViewModel to Channels collection: {channel.Key}");
        }

        Debug.WriteLine($"[DEBUG] LoadChannelsFromFixture complete. Total channels loaded: {Channels.Count}");
    }

    private async void ChannelViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.WriteLine($"[DEBUG] ChannelViewModel_PropertyChanged called. Property: {e.PropertyName}, Sender: {sender?.GetType().Name}");
        if (e.PropertyName == nameof(ChannelViewModel.Value) && sender is ChannelViewModel channelVm)
        {
            Debug.WriteLine($"[DEBUG] Channel value changed: {channelVm.Name} = {channelVm.Value}");

            // Update the fixture model
            _currentFixture.Channels[channelVm.Name] = channelVm.Value;
            Debug.WriteLine($"[DEBUG] Updated fixture model: {_currentFixture.FixtureId}.{channelVm.Name} = {channelVm.Value}");

            // Send to hardware connection
            Debug.WriteLine($"[DEBUG] About to call SetChannelValueAsync with:");
            Debug.WriteLine($"[DEBUG]   - fixtureInstanceId: '{_currentFixture.FixtureId}'");
            Debug.WriteLine($"[DEBUG]   - channelName: '{channelVm.Name}'");
            Debug.WriteLine($"[DEBUG]   - value: {channelVm.Value}");

            var result = await _hardwareConnection.SetChannelValueAsync(
                _currentFixture.FixtureId,
                channelVm.Name,
                channelVm.Value);

            Debug.WriteLine($"[DEBUG] SetChannelValueAsync returned: {result}");
        }
        else
        {
            Debug.WriteLine($"[DEBUG] ChannelViewModel_PropertyChanged ignored - not a Value change or invalid sender");
        }
    }

    private string FindScenesFile()
    {
        // Try to find project root by searching upwards from BaseDirectory
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        Debug.WriteLine($"[DEBUG] Starting search from: {currentDir.FullName}");

        while (currentDir != null)
        {
            var scenesPath = Path.Combine(currentDir.FullName, "scenes.json");
            Debug.WriteLine($"[DEBUG] Checking: {scenesPath}");

            if (File.Exists(scenesPath))
            {
                Debug.WriteLine($"[DEBUG] ✓ Found scenes.json in: {scenesPath}");
                return scenesPath;
            }

            // Also check if this directory contains the .sln file (project root indicator)
            var slnFiles = currentDir.GetFiles("*.sln");
            if (slnFiles.Length > 0)
            {
                Debug.WriteLine($"[DEBUG] Found .sln file in: {currentDir.FullName}");
                var projectRootScenes = Path.Combine(currentDir.FullName, "scenes.json");

                if (File.Exists(projectRootScenes))
                {
                    Debug.WriteLine($"[DEBUG] ✓ Found scenes.json at project root: {projectRootScenes}");
                    return projectRootScenes;
                }

                Debug.WriteLine($"[DEBUG] No scenes.json at project root, will create it");

                // Create default scenes.json at project root
                var defaultContent = @"{
  ""scene"": {
    ""id"": ""default"",
    ""name"": ""Default"",
    ""universe"": 1,
    ""fixtures"": []
  }
}";
                File.WriteAllText(projectRootScenes, defaultContent);
                return projectRootScenes;
            }

            currentDir = currentDir.Parent;
        }

        Debug.WriteLine($"[DEBUG] Could not find project root, using AppData");

        // Fallback to AppData
        var appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InterdisciplinairProject");
        Directory.CreateDirectory(appFolder);
        var appDataScenesPath = Path.Combine(appFolder, "scenes.json");

        Debug.WriteLine($"[DEBUG] Using AppData scenes path: {appDataScenesPath}");
        return appDataScenesPath;
    }

    private Fixture? LoadFirstFixtureFromScenes(string scenesFilePath)
    {
        try
        {
            if (!File.Exists(scenesFilePath))
            {
                Debug.WriteLine($"[DEBUG] Scenes file does not exist: {scenesFilePath}");
                return null;
            }

            var json = File.ReadAllText(scenesFilePath);
            Debug.WriteLine($"[DEBUG] Read scenes.json content: {json.Substring(0, Math.Min(200, json.Length))}...");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Navigate to scene.fixtures[0]
            if (!root.TryGetProperty("scene", out var sceneElement))
            {
                Debug.WriteLine("[DEBUG] No 'scene' property found in scenes.json");
                return null;
            }

            if (!sceneElement.TryGetProperty("fixtures", out var fixturesElement))
            {
                Debug.WriteLine("[DEBUG] No 'fixtures' array found in scene");
                return null;
            }

            if (fixturesElement.ValueKind != JsonValueKind.Array || fixturesElement.GetArrayLength() == 0)
            {
                Debug.WriteLine("[DEBUG] Fixtures array is empty or invalid");
                return null;
            }

            var firstFixture = fixturesElement[0];

            // Parse fixture properties
            var fixtureId = firstFixture.TryGetProperty("fixtureId", out var fidProp) ? fidProp.GetString() : "unknown";
            var instanceId = firstFixture.TryGetProperty("instanceId", out var iidProp) ? iidProp.GetString() : fixtureId;
            var name = firstFixture.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : "Fixture";

            var channels = new Dictionary<string, byte?>();

            if (firstFixture.TryGetProperty("channels", out var channelsElement))
            {
                foreach (var channelProp in channelsElement.EnumerateObject())
                {
                    byte? value = null;
                    if (channelProp.Value.ValueKind == JsonValueKind.Number)
                    {
                        value = (byte)channelProp.Value.GetInt32();
                    }

                    channels[channelProp.Name] = value ?? 0;
                    Debug.WriteLine($"[DEBUG] Loaded channel: {channelProp.Name} = {value ?? 0}");
                }
            }

            var fixture = new Fixture
            {
                FixtureId = instanceId ?? fixtureId ?? "unknown",
                Name = name ?? "Fixture",
                Channels = channels,
            };

            Debug.WriteLine($"[DEBUG] Successfully loaded fixture from scenes.json: {fixture.Name}");
            return fixture;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] Error loading fixture from scenes.json: {ex.Message}");
            return null;
        }
    }

    private Fixture CreateDefaultFixture()
    {
        Debug.WriteLine("[DEBUG] CreateDefaultFixture called - will create fixture in scenes.json");

        var defaultFixture = new Fixture
        {
            FixtureId = "fixture-inst-default",
            Name = "Default Wash Light",
            Channels = new Dictionary<string, byte?>
            {
                { "dimmer", 0 },
                { "red", 0 },
                { "green", 0 },
                { "blue", 0 },
                { "strobe", 0 },
                { "pan", 0 },
                { "tilt", 0 },
            },
        };

        // Create the scenes.json file with this fixture
        var scenesFilePath = FindScenesFile();
        Debug.WriteLine($"[DEBUG] Writing default fixture to: {scenesFilePath}");

        try
        {
            var scenesContent = new
            {
                scene = new
                {
                    id = "default-scene",
                    name = "Default Scene",
                    universe = 1,
                    fixtures = new[]
                    {
                        new
                        {
                            fixtureId = "default-wash",
                            instanceId = defaultFixture.FixtureId,
                            name = defaultFixture.Name,
                            channels = defaultFixture.Channels.ToDictionary(
                                kvp => kvp.Key,
                                kvp => (int)(kvp.Value ?? 0)),
                        },
                    },
                },
            };

            var json = JsonSerializer.Serialize(scenesContent, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(scenesFilePath, json);
            Debug.WriteLine($"[DEBUG] ✓ Created scenes.json with default fixture at: {scenesFilePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] ERROR creating scenes.json: {ex.Message}");
        }

        return defaultFixture;
    }
}
