using System.Diagnostics;
using System.IO;
using System.Text.Json;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Services;

/// <summary>
/// Manages the active scene and provides persistence.
/// </summary>
public class SceneManager
{
    private const string ScenesFileName = "scenes.json";

    private static readonly Lazy<SceneManager> InstanceBacking = new(() => new SceneManager());

    private readonly string _scenesFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneManager"/> class.
    /// </summary>
    public SceneManager()
    {
        _scenesFilePath = DetermineScenesFilePath();
        CurrentScene = LoadSceneFromFile() ?? CreateDefaultScene();
        Debug.WriteLine($"[DEBUG] SceneManager: Loaded scene '{CurrentScene.Name}' from '{_scenesFilePath}'");
    }

    /// <summary>
    /// Fired when the scene data changes and is saved.
    /// </summary>
    public event EventHandler? SceneUpdated;

    /// <summary>
    /// Gets the shared instance of the scene manager.
    /// </summary>
    public static SceneManager Instance => InstanceBacking.Value;

    /// <summary>
    /// Gets the current scene loaded in memory.
    /// </summary>
    public Scene CurrentScene { get; private set; }

    /// <summary>
    /// Adds a fixture to the current scene as a new instance.
    /// </summary>
    /// <param name="fixture">The fixture definition to add to the scene.</param>
    public void AddFixtureToScene(Fixture fixture)
    {
        var instance = new Fixture
        {
            FixtureId = fixture.FixtureId,
            InstanceId = Guid.NewGuid().ToString("N"),
            Name = fixture.Name,
            Manufacturer = fixture.Manufacturer,
            Description = fixture.Description,
            Channels = new Dictionary<string, byte?>(fixture.Channels),
            ChannelDescriptions = new Dictionary<string, string>(fixture.ChannelDescriptions),
            ChannelTypes = new Dictionary<string, ChannelType>(fixture.ChannelTypes),
            ChannelEffects = new Dictionary<string, List<ChannelEffect>>(fixture.ChannelEffects),
            StartAddress = fixture.StartAddress,
        };

        CurrentScene.Fixtures?.Add(instance);
        Debug.WriteLine($"[DEBUG] SceneManager: Added fixture '{instance.Name}' ({instance.InstanceId}) to scene '{CurrentScene.Name}' ({CurrentScene.Id})");
        SaveSceneToFile();
    }

    private Scene? LoadSceneFromFile()
    {
        try
        {
            if (!File.Exists(_scenesFilePath))
            {
                Debug.WriteLine($"[DEBUG] SceneManager: scenes.json not found at {_scenesFilePath}");
                return null;
            }

            var json = File.ReadAllText(_scenesFilePath);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("scene", out var sceneEl))
            {
                var id = sceneEl.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty;
                var name = sceneEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                var universe = sceneEl.TryGetProperty("universe", out var uEl) ? uEl.GetInt32() : 1;

                var scene = new Scene { Id = id, Name = name, Universe = universe, Fixtures = new List<Fixture>() };

                if (sceneEl.TryGetProperty("fixtures", out var fixturesEl) && fixturesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var fixEl in fixturesEl.EnumerateArray())
                    {
                        try
                        {
                            var fixtureId = fixEl.TryGetProperty("fixtureId", out var fId) ? fId.GetString() ?? string.Empty : string.Empty;
                            var instanceId = fixEl.TryGetProperty("instanceId", out var iId) ? iId.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
                            var nameProp = fixEl.TryGetProperty("name", out var nEl) ? nEl.GetString() ?? string.Empty : string.Empty;

                            var sceneFix = new Fixture
                            {
                                FixtureId = fixtureId,
                                InstanceId = instanceId,
                                Name = nameProp,
                                Channels = new Dictionary<string, byte?>(),
                            };

                            if (fixEl.TryGetProperty("channels", out var channelsEl) && channelsEl.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var chan in channelsEl.EnumerateObject())
                                {
                                    if (chan.Value.ValueKind == JsonValueKind.Number)
                                    {
                                        var intVal = chan.Value.GetInt32();
                                        sceneFix.Channels[chan.Name] = (byte)Math.Max(0, Math.Min(255, intVal));
                                    }
                                    else
                                    {
                                        sceneFix.Channels[chan.Name] = 0;
                                    }
                                }
                            }

                            scene.Fixtures.Add(sceneFix);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[DEBUG] SceneManager: error parsing fixture entry: {ex.Message}");
                        }
                    }
                }

                return scene;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] SceneManager: Error loading scene: {ex.Message}");
        }

        return null;
    }

    private Scene CreateDefaultScene()
    {
        var defaultScene = new Scene { Id = "default", Name = "Default Scene", Universe = 1, Fixtures = new List<Fixture>() };
        return defaultScene;
    }

    private string DetermineScenesFilePath()
    {
        // Prioritize AppData: Check if scenes.json exists there first
        var appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InterdisciplinairProject");
        Directory.CreateDirectory(appFolder);
        var appDataScenesPath = Path.Combine(appFolder, ScenesFileName);
        if (File.Exists(appDataScenesPath))
        {
            Debug.WriteLine($"[DEBUG] SceneManager: Found existing scenes.json in AppData: {appDataScenesPath}");
            return appDataScenesPath;
        }

        try
        {
            // If not in AppData, look for an existing scenes.json in the app folder tree (project root etc.)
            var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
            while (currentDir != null)
            {
                var scenesPath = Path.Combine(currentDir.FullName, ScenesFileName);
                if (File.Exists(scenesPath))
                {
                    Debug.WriteLine($"[DEBUG] SceneManager: Found existing scenes.json in directory tree: {scenesPath}");
                    return scenesPath;
                }

                currentDir = currentDir.Parent;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] SceneManager: DetermineScenesFilePath error while searching directory tree: {ex.Message}");
        }

        // If still not found, create a default one in AppData
        try
        {
            var defaultContent = new
            {
                scene = new
                {
                    id = "default",
                    name = "Default",
                    universe = 1,
                    fixtures = new object[] { },
                },
            };

            var json = JsonSerializer.Serialize(defaultContent, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(appDataScenesPath, json);
            Debug.WriteLine($"[DEBUG] SceneManager: Created default scenes.json in AppData: {appDataScenesPath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] SceneManager: Error creating default scenes.json in AppData: {ex.Message}");
        }

        Debug.WriteLine($"[DEBUG] SceneManager: Using AppData scenes path: {appDataScenesPath}");
        return appDataScenesPath;
    }

    private void SaveSceneToFile()
    {
        try
        {
            if (string.IsNullOrEmpty(_scenesFilePath))
            {
                Debug.WriteLine("[DEBUG] SceneManager: scenes file path is not set. Skipping save.");
                return;
            }

            var scenesContent = new
            {
                scene = new
                {
                    id = CurrentScene.Id,
                    name = CurrentScene.Name,
                    universe = CurrentScene.Universe,
                    fixtures = CurrentScene.Fixtures?.Select(f => new
                    {
                        fixtureId = f.FixtureId,
                        instanceId = f.InstanceId,
                        name = f.Name,
                        channels = f.Channels.ToDictionary(kvp => kvp.Key, kvp => (int)(kvp.Value ?? 0)),
                    }).ToArray() ?? Array.Empty<object>(),
                },
            };

            var json = JsonSerializer.Serialize(scenesContent, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_scenesFilePath, json);
            Debug.WriteLine($"[DEBUG] SceneManager: Saved scene to {_scenesFilePath}");
            SceneUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] SceneManager: Error saving scene to file: {ex.Message}");
        }
    }
}
