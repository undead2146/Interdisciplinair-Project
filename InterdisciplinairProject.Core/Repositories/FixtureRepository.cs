using InterdisciplinairProject.Core.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InterdisciplinairProject.Core.Interfaces;

namespace InterdisciplinairProject.Core.Repositories;

/// <summary>
/// Repository for fixture data loaded from JSON.
/// </summary>
public class FixtureRepository : IFixtureRepository
{
    private readonly string _fixturesDirectoryPath;
    private List<FixtureDefinition> _cachedFixtures;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRepository"/> class.
    /// </summary>
    /// <param name="fixturesDirectoryPath">The path to the fixtures directory.</param>
    public FixtureRepository(string fixturesDirectoryPath)
    {
        _fixturesDirectoryPath = fixturesDirectoryPath;
        _cachedFixtures = new List<FixtureDefinition>();
    }

    /// <summary>
    /// Gets all fixtures from the JSON files in the fixtures directory.
    /// </summary>
    /// <returns>A list of fixtures.</returns>
    public async Task<List<FixtureDefinition>> GetAllFixturesAsync()
    {
        var fixtures = new List<FixtureDefinition>();

        if (!Directory.Exists(_fixturesDirectoryPath))
        {
            return fixtures;
        }

        // Get all manufacturer directories
        var manufacturerDirs = Directory.GetDirectories(_fixturesDirectoryPath);
        foreach (var manufacturerDir in manufacturerDirs)
        {
            // Get all fixture JSON files in this manufacturer directory
            var fixtureFiles = Directory.GetFiles(manufacturerDir, "*.json");
            foreach (var fixtureFile in fixtureFiles)
            {
                try
                {
                    var fixture = await LoadFixtureFromFileAsync(fixtureFile);
                    if (fixture != null)
                    {
                        fixtures.Add(fixture);
                        Debug.WriteLine($"[DEBUG] FixtureRepository: Loaded fixture '{fixture.Name}' with Id '{fixture.Id}' and {fixture.Channels.Count} channels");
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }
        }

        // Deduplicate by Id
        _cachedFixtures = fixtures
            .GroupBy(f => f.Id)
            .Select(g => g.First())
            .ToList();

        Debug.WriteLine($"[DEBUG] FixtureRepository: Total unique fixtures after deduplication: {_cachedFixtures.Count}");
        return _cachedFixtures;
    }

    /// <summary>
    /// Gets a fixture by its ID.
    /// </summary>
    /// <param name="fixtureId">The fixture ID.</param>
    /// <returns>The fixture, or null if not found.</returns>
    public FixtureDefinition? GetFixtureById(string fixtureId)
    {
        // First check cached fixtures
        var fixture = _cachedFixtures.FirstOrDefault(f => f.FixtureId == fixtureId);
        if (fixture != null)
        {
            Debug.WriteLine($"[DEBUG] FixtureRepository: GetFixtureById('{fixtureId}') found in cache: '{fixture.Name}' with {fixture.Channels.Count} channels");
            return fixture;
        }

        // If not in cache, try to load from files
        // This is a fallback, normally fixtures should be loaded via GetAllFixturesAsync
        if (Directory.Exists(_fixturesDirectoryPath))
        {
            var manufacturerDirs = Directory.GetDirectories(_fixturesDirectoryPath);
            foreach (var manufacturerDir in manufacturerDirs)
            {
                var fixtureFiles = Directory.GetFiles(manufacturerDir, "*.json");
                foreach (var fixtureFile in fixtureFiles)
                {
                    try
                    {
                        var loadedFixture = LoadFixtureFromFileAsync(fixtureFile).Result;
                        if (loadedFixture != null && loadedFixture.Id == fixtureId)
                        {
                            Debug.WriteLine($"[DEBUG] FixtureRepository: GetFixtureById('{fixtureId}') loaded from file: '{loadedFixture.Name}' with {loadedFixture.Channels.Count} channels");
                            return loadedFixture;
                        }
                    }
                    catch
                    {
                        // Skip
                    }
                }
            }
        }

        Debug.WriteLine($"[DEBUG] FixtureRepository: GetFixtureById('{fixtureId}') not found");
        return null;
    }

    private async Task<FixtureDefinition?> LoadFixtureFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        Debug.WriteLine($"[DEBUG] FixtureRepository: Loading fixture from {filePath}");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var name = root.GetProperty("name").GetString() ?? string.Empty;
        var manufacturer = root.GetProperty("manufacturer").GetString() ?? string.Empty;
        var imagePath = root.TryGetProperty("imagePath", out var ip) ? ip.GetString() : string.Empty;

        var channels = new Dictionary<string, byte?>();
        var channelDescriptions = new Dictionary<string, string>();
        var channelTypes = new Dictionary<string, ChannelType>();

        if (root.TryGetProperty("channels", out var channelsElement))
        {
            if (channelsElement.ValueKind == JsonValueKind.Array)
            {
                Debug.WriteLine($"[DEBUG] FixtureRepository: Parsing array format for {name}");

                // Array format: channels: [{Name, Type, value}, ...]
                foreach (var channelElement in channelsElement.EnumerateArray())
                {
                    var channelName = channelElement.GetProperty("Name").GetString() ?? string.Empty;
                    var channelTypeStr = channelElement.GetProperty("Type").GetString() ?? string.Empty;
                    var valueStr = channelElement.GetProperty("value").GetString() ?? "0";
                    if (byte.TryParse(valueStr, out var value))
                    {
                        channels[channelName] = value;
                    }
                    else
                    {
                        channels[channelName] = 0;
                    }

                    var channelType = Enum.TryParse<ChannelType>(channelTypeStr, true, out var parsedType) ? parsedType : ChannelType.Unknown;
                    channelTypes[channelName] = channelType;

                    // Create description from type
                    channelDescriptions[channelName] = $"{channelName}: {channelTypeStr}";
                    Debug.WriteLine($"[DEBUG] FixtureRepository: Added channel '{channelName}' = {channels[channelName]}, Type: {channelType}");
                }
            }
            else if (channelsElement.ValueKind == JsonValueKind.Object)
            {
                // Dictionary format: channels: {name: value, ...}
                foreach (var prop in channelsElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        channels[prop.Name] = (byte)prop.Value.GetInt32();
                    }
                    else
                    {
                        channels[prop.Name] = null;
                    }

                    channelTypes[prop.Name] = Enum.TryParse<ChannelType>(prop.Name, true, out var parsedType) ? parsedType : ChannelType.Unknown;
                }
            }
        }

        var fixture = new FixtureDefinition
        {
            FixtureId = name, // Use name as FixtureId for now
            Name = name,
            Manufacturer = manufacturer,
            Channels = channels,
            ChannelDescriptions = channelDescriptions,
            ChannelTypes = channelTypes,
        };
        Debug.WriteLine($"[DEBUG] FixtureRepository: Created fixture '{fixture.Name}' with Id '{fixture.Id}' and channels: {string.Join(", ", fixture.Channels.Keys)}");
        return fixture;
    }

    private class FixturesData
    {
        public List<Fixture> Fixtures { get; set; } = new();
    }
}
