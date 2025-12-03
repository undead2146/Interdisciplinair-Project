using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Features.Scene;

/// <summary>
/// Service for managing the fixture registry.
/// Handles importing, storing, and managing fixture instances.
/// </summary>
public class FixtureRegistry : IFixtureRegistry
{
    private const string AppDataFolder = "InterdisciplinairProject";
    private const string RegistryFileName = "fixture_registry.json";

    private readonly string _registryPath;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly object _lock = new object();
    private List<Core.Models.Fixture> _cachedFixtures;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRegistry"/> class.
    /// </summary>
    /// <param name="fixtureRepository">The fixture repository for loading fixture data.</param>
    public FixtureRegistry(IFixtureRepository fixtureRepository)
    {
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataFolder);

        Directory.CreateDirectory(appDataPath);
        _registryPath = Path.Combine(appDataPath, RegistryFileName);
        _cachedFixtures = new List<Core.Models.Fixture>();

        Debug.WriteLine($"[DEBUG] FixtureRegistry: Registry path set to: {_registryPath}");
    }

    /// <inheritdoc/>
    public async Task<List<Core.Models.Fixture>> GetAllFixturesAsync()
    {
        await EnsureRegistryLoadedAsync();
        lock (_lock)
        {
            return new List<InterdisciplinairProject.Core.Models.Fixture>(_cachedFixtures);
        }
    }

    /// <inheritdoc/>
    public Core.Models.Fixture? GetFixtureById(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return null;
        }

        lock (_lock)
        {
            return _cachedFixtures.FirstOrDefault(f => f.InstanceId == instanceId);
        }
    }

    /// <inheritdoc/>
    public async Task<int> ImportFixturesAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[DEBUG] FixtureRegistry: File not found: {filePath}");
                return 0;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var fixtures = ParseMultipleFixtures(json);

            if (fixtures == null || fixtures.Count == 0)
            {
                Debug.WriteLine("[DEBUG] FixtureRegistry: No fixtures found in file");
                return 0;
            }

            int imported = 0;
            foreach (var fixture in fixtures)
            {
                if (await AddFixtureAsync(fixture))
                {
                    imported++;
                }
            }

            Debug.WriteLine($"[DEBUG] FixtureRegistry: Imported {imported} out of {fixtures.Count} fixtures");
            return imported;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Error importing fixtures: {ex.Message}");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AddFixtureAsync(Core.Models.Fixture fixture)
    {
        if (fixture == null)
        {
            Debug.WriteLine("[DEBUG] FixtureRegistry: Invalid fixture");
            return false;
        }

        if (string.IsNullOrWhiteSpace(fixture.InstanceId))
        {
            fixture.InstanceId = Guid.NewGuid().ToString();
        }

        await EnsureRegistryLoadedAsync();

        lock (_lock)
        {
            // Check if fixture already exists
            if (_cachedFixtures.Any(f => f.InstanceId == fixture.InstanceId))
            {
                Debug.WriteLine($"[DEBUG] FixtureRegistry: Fixture '{fixture.InstanceId}' already exists in registry");
                return false;
            }

            _cachedFixtures.Add(fixture);
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Added fixture '{fixture.Name}' to registry");
        }

        await SaveRegistryAsync();
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveFixtureAsync(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return false;
        }

        await EnsureRegistryLoadedAsync();

        bool removed;
        lock (_lock)
        {
            var fixture = _cachedFixtures.FirstOrDefault(f => f.InstanceId == instanceId);
            if (fixture == null)
            {
                Debug.WriteLine($"[DEBUG] FixtureRegistry: CoreFixture '{instanceId}' not found in registry");
                return false;
            }

            removed = _cachedFixtures.Remove(fixture);
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Removed fixture '{instanceId}' from registry");
        }

        if (removed)
        {
            await SaveRegistryAsync();
        }

        return removed;
    }

    /// <inheritdoc/>
    public Core.Models.Fixture? CreateFixtureInstance(string fixtureId, string instanceId, int startAddress)
    {
        var definition = _fixtureRepository.GetFixtureById(fixtureId);
        if (definition == null)
        {
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Cannot create instance - fixture definition '{fixtureId}' not found");
            return null;
        }

        var instance = new Core.Models.Fixture
        {
            FixtureId = definition.FixtureId,
            InstanceId = instanceId,
            Name = definition.Name,
            Manufacturer = definition.Manufacturer,
            Description = definition.Description,
            Channels = new ObservableCollection<Channel>(definition.Channels.Select(c => new Channel
            {
                Name = c.Name,
                Type = c.Type,
                Value = c.Value,
                Parameter = c.Parameter,
                Min = c.Min,
                Max = c.Max,
                Time = c.Time,
                ChannelEffect = c.ChannelEffect,
            })),
            ChannelDescriptions = new Dictionary<string, string>(definition.ChannelDescriptions),
            ChannelTypes = new Dictionary<string, ChannelType>(), // TODO: set properly
            StartAddress = startAddress,
        };

        Debug.WriteLine($"[DEBUG] FixtureRegistry: Created instance '{instanceId}' from definition '{fixtureId}'");
        return instance;
    }

    /// <inheritdoc/>
    public async Task RefreshRegistryAsync()
    {
        await LoadRegistryAsync();
        Debug.WriteLine("[DEBUG] FixtureRegistry: Registry refreshed");
    }

    /// <inheritdoc/>
    public bool FixtureExists(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return false;
        }

        lock (_lock)
        {
            return _cachedFixtures.Any(f => f.InstanceId == instanceId);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExportFixturesAsync(string filePath)
    {
        try
        {
            List<Core.Models.Fixture> toExport;
            lock (_lock)
            {
                toExport = new List<Core.Models.Fixture>(_cachedFixtures);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            var exportData = new
            {
                version = "1.0",
                exportedAt = DateTime.UtcNow,
                fixtures = toExport.Select(f => new
                {
                    fixtureId = f.FixtureId,
                    instanceId = f.InstanceId,
                    name = f.Name,
                    manufacturer = f.Manufacturer,
                    description = f.Description,
                    channels = f.Channels,
                    channelDescriptions = f.ChannelDescriptions,
                    startAddress = f.StartAddress,
                }),
            };

            var json = JsonSerializer.Serialize(exportData, options);
            await File.WriteAllTextAsync(filePath, json);

            Debug.WriteLine($"[DEBUG] FixtureRegistry: Exported {toExport.Count} fixtures to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Error exporting fixtures: {ex.Message}");
            return false;
        }
    }

    private async Task EnsureRegistryLoadedAsync()
    {
        lock (_lock)
        {
            if (_cachedFixtures.Count > 0)
            {
                return;
            }
        }

        await LoadRegistryAsync();
    }

    private async Task LoadRegistryAsync()
    {
        try
        {
            // First, try to load from the registry file
            var fixtures = new List<Core.Models.Fixture>();

            if (File.Exists(_registryPath))
            {
                var json = await File.ReadAllTextAsync(_registryPath);
                var loadedFixtures = ParseMultipleFixtures(json);
                if (loadedFixtures != null)
                {
                    fixtures.AddRange(loadedFixtures);
                    Debug.WriteLine($"[DEBUG] FixtureRegistry: Loaded {fixtures.Count} fixtures from registry file");
                }
            }
            else
            {
                Debug.WriteLine("[DEBUG] FixtureRegistry: No registry file found, starting with empty registry");
            }

            lock (_lock)
            {
                _cachedFixtures = fixtures;
                Debug.WriteLine($"[DEBUG] FixtureRegistry: Total fixtures in registry: {_cachedFixtures.Count}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Error loading registry: {ex.Message}");
        }
    }

    private async Task SaveRegistryAsync()
    {
        try
        {
            List<Core.Models.Fixture> toSave;
            lock (_lock)
            {
                toSave = new List<Core.Models.Fixture>(_cachedFixtures);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            var registryData = new
            {
                version = "1.0",
                lastModified = DateTime.UtcNow,
                fixtures = toSave.Select(f => new
                {
                    fixtureId = f.FixtureId,
                    instanceId = f.InstanceId,
                    name = f.Name,
                    manufacturer = f.Manufacturer,
                    description = f.Description,
                    channels = f.Channels,
                    channelDescriptions = f.ChannelDescriptions,
                    startAddress = f.StartAddress,
                }),
            };

            var json = JsonSerializer.Serialize(registryData, options);
            await File.WriteAllTextAsync(_registryPath, json);

            Debug.WriteLine($"[DEBUG] FixtureRegistry: Saved {toSave.Count} fixtures to registry");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Error saving registry: {ex.Message}");
        }
    }

    private List<Core.Models.Fixture> ParseMultipleFixtures(string json)
    {
        var fixtures = new List<Core.Models.Fixture>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("fixtures", out var fixturesArray))
            {
                // Collection format: { "fixtures": [...] }
                foreach (var fixtureElement in fixturesArray.EnumerateArray())
                {
                    var fixture = ParseFixtureElement(fixtureElement);
                    if (fixture != null)
                    {
                        fixtures.Add(fixture);
                    }
                }
            }
            else if (root.TryGetProperty("fixtureId", out _) || root.TryGetProperty("instanceId", out _))
            {
                // Single fixture format
                var fixture = ParseFixtureElement(root);
                if (fixture != null)
                {
                    fixtures.Add(fixture);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Error parsing fixtures: {ex.Message}");
        }

        return fixtures;
    }

    private Core.Models.Fixture? ParseFixtureElement(JsonElement element)
    {
        try
        {
            var fixtureId = element.TryGetProperty("fixtureId", out var fidProp) ? fidProp.GetString() : null;
            var instanceId = element.TryGetProperty("instanceId", out var iidProp) ? iidProp.GetString() : null;
            var name = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            var manufacturer = element.TryGetProperty("manufacturer", out var mfgProp) ? mfgProp.GetString() : string.Empty;
            var description = element.TryGetProperty("description", out var descProp) ? descProp.GetString() : string.Empty;
            var startAddress = element.TryGetProperty("startAddress", out var saProp) && saProp.TryGetInt32(out var sa) ? sa : 1;

            var channels = new ObservableCollection<Channel>();
            var channelDescriptions = new Dictionary<string, string>();

            if (element.TryGetProperty("channels", out var channelsElement))
            {
                if (channelsElement.ValueKind == JsonValueKind.Array)
                {
                    // Array format: [{Name, Type, value}, ...]
                    var list = JsonSerializer.Deserialize<List<Channel>>(channelsElement.GetRawText());
                    if (list != null)
                    {
                        foreach (var c in list)
                        {
                            channels.Add(c);
                            channelDescriptions[c.Name] = $"{c.Name}: {c.Type}";
                        }
                    }
                }
                else if (channelsElement.ValueKind == JsonValueKind.Object)
                {
                    // Dictionary format: {name: value, ...}
                    foreach (var prop in channelsElement.EnumerateObject())
                    {
                        var ch = new Channel
                        {
                            Name = prop.Name,
                            Value = prop.Value.ToString(),
                            Type = "Unknown",
                            Min = 0,
                            Max = 255,
                            Time = 0,
                            ChannelEffect = new ChannelEffect(),
                        };
                        channels.Add(ch);
                        channelDescriptions[prop.Name] = $"{prop.Name}: Unknown";
                    }
                }
            }

            if (element.TryGetProperty("channelDescriptions", out var channelDescsElement))
            {
                foreach (var channelDescProp in channelDescsElement.EnumerateObject())
                {
                    if (channelDescProp.Value.ValueKind == JsonValueKind.String)
                    {
                        channelDescriptions[channelDescProp.Name] = channelDescProp.Value.GetString() ?? string.Empty;
                    }
                }
            }

            return new Core.Models.Fixture
            {
                FixtureId = fixtureId ?? string.Empty,
                InstanceId = instanceId,
                Name = name ?? instanceId,
                Manufacturer = manufacturer ?? string.Empty,
                Description = description ?? string.Empty,
                Channels = channels,
                ChannelDescriptions = channelDescriptions,
                StartAddress = startAddress,
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRegistry: Error parsing fixture element: {ex.Message}");
            return null;
        }
    }
}
