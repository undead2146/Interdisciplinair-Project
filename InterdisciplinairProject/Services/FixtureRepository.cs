using System.Diagnostics;
using System.IO;
using System.Text.Json;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Services;

/// <summary>
/// Repository for managing fixture data from various sources.
/// </summary>
public class FixtureRepository
{
    private const string AppDataFolder = "InterdisciplinairProject";
    private const string FixturesFileName = "fixtures.json";

    private readonly string _appDataPath;
    private List<Fixture> _cachedFixtures;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRepository"/> class.
    /// </summary>
    public FixtureRepository()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataFolder);

        Debug.WriteLine($"[DEBUG] FixtureRepository: AppData path set to: {_appDataPath}");
        Directory.CreateDirectory(_appDataPath);
        _cachedFixtures = new List<Fixture>();
    }

    /// <summary>
    /// Gets all available fixtures from all sources.
    /// </summary>
    /// <returns>A list of all available fixtures.</returns>
    public async Task<List<Fixture>> GetAllFixturesAsync()
    {
        var fixtures = new List<Fixture>();

        Debug.WriteLine($"[DEBUG] FixtureRepository: Starting GetAllFixturesAsync");
        Debug.WriteLine($"[DEBUG] FixtureRepository: AppData path: {_appDataPath}");

        // Load from AppData
        var appDataFixtures = await LoadFixturesFromAppDataAsync();
        fixtures.AddRange(appDataFixtures);
        Debug.WriteLine($"[DEBUG] FixtureRepository: Loaded {appDataFixtures.Count} fixtures from AppData");

        // Load from project data folder (if exists)
        var projectFixtures = await LoadFixturesFromProjectDataAsync();
        fixtures.AddRange(projectFixtures);
        Debug.WriteLine($"[DEBUG] FixtureRepository: Loaded {projectFixtures.Count} fixtures from project data");

        // Deduplicate by FixtureId
        _cachedFixtures = fixtures
            .GroupBy(f => f.Id)
            .Select(g => g.First())
            .ToList();

        Debug.WriteLine($"[DEBUG] FixtureRepository: Total unique fixtures after deduplication: {_cachedFixtures.Count}");
        return _cachedFixtures;
    }

    /// <summary>
    /// Imports a fixture from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>True if import was successful.</returns>
    public async Task<bool> ImportFixtureFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"[DEBUG] FixtureRepository: File not found: {filePath}");
                return false;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var fixtures = ParseFixturesFromJson(json);

            if (fixtures == null || fixtures.Count == 0)
            {
                Debug.WriteLine("[DEBUG] FixtureRepository: No fixtures found in file");
                return false;
            }

            // Save to AppData
            await SaveFixturesToAppDataAsync(fixtures);

            // Reload cache
            await GetAllFixturesAsync();

            Debug.WriteLine($"[DEBUG] FixtureRepository: Imported {fixtures.Count} fixtures from {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRepository: Error importing fixture: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets a fixture by its ID.
    /// </summary>
    /// <param name="fixtureId">The fixture ID.</param>
    /// <returns>The fixture, or null if not found.</returns>
    public Fixture? GetFixtureById(string fixtureId)
    {
        return _cachedFixtures.FirstOrDefault(f => f.Id == fixtureId);
    }

    private async Task<List<Fixture>> LoadFixturesFromAppDataAsync()
    {
        var fixturesPath = Path.Combine(_appDataPath, FixturesFileName);

        if (!File.Exists(fixturesPath))
        {
            Debug.WriteLine($"[DEBUG] FixtureRepository: No fixtures file in AppData: {fixturesPath}");
            return new List<Fixture>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(fixturesPath);
            var fixtures = ParseFixturesFromJson(json);
            Debug.WriteLine($"[DEBUG] FixtureRepository: Loaded {fixtures.Count} fixtures from AppData");
            return fixtures;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRepository: Error loading AppData fixtures: {ex.Message}");
            return new List<Fixture>();
        }
    }

    private async Task<List<Fixture>> LoadFixturesFromProjectDataAsync()
    {
        Debug.WriteLine($"[DEBUG] FixtureRepository: LoadFixturesFromProjectDataAsync started");
        Debug.WriteLine($"[DEBUG] FixtureRepository: AppContext.BaseDirectory: {AppContext.BaseDirectory}");

        // Try to find project root
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        Debug.WriteLine($"[DEBUG] FixtureRepository: Starting directory: {currentDir.FullName}");

        while (currentDir != null)
        {
            Debug.WriteLine($"[DEBUG] FixtureRepository: Checking directory: {currentDir.FullName}");
            var slnFiles = currentDir.GetFiles("*.sln");
            if (slnFiles.Length > 0)
            {
                Debug.WriteLine($"[DEBUG] FixtureRepository: Found solution file: {slnFiles[0].Name}");
                var dataPath = Path.Combine(currentDir.FullName, "InterdisciplinairProject.Features", "Scene", "data", FixturesFileName);
                Debug.WriteLine($"[DEBUG] FixtureRepository: Trying data path: {dataPath}");

                if (File.Exists(dataPath))
                {
                    Debug.WriteLine($"[DEBUG] FixtureRepository: Found fixtures file at: {dataPath}");
                    try
                    {
                        var json = await File.ReadAllTextAsync(dataPath);
                        var fixtures = ParseFixturesFromJson(json);
                        Debug.WriteLine($"[DEBUG] FixtureRepository: Loaded {fixtures.Count} fixtures from project data");
                        return fixtures;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[DEBUG] FixtureRepository: Error loading project fixtures: {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine($"[DEBUG] FixtureRepository: Fixtures file not found at: {dataPath}");
                }

                break;
            }

            currentDir = currentDir.Parent;
        }

        Debug.WriteLine($"[DEBUG] FixtureRepository: No fixtures found in project data");
        return new List<Fixture>();
    }

    private List<Fixture> ParseFixturesFromJson(string json)
    {
        var fixtures = new List<Fixture>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Check if it's a single fixture or a collection
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
            else if (root.TryGetProperty("fixtureId", out _))
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
            Debug.WriteLine($"[DEBUG] FixtureRepository: Error parsing JSON: {ex.Message}");
        }

        return fixtures;
    }

    private Fixture? ParseFixtureElement(JsonElement element)
    {
        try
        {
            var fixtureId = element.TryGetProperty("fixtureId", out var fidProp) ? fidProp.GetString() : null;
            var name = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            var manufacturer = element.TryGetProperty("manufacturer", out var mfgProp) ? mfgProp.GetString() : string.Empty;
            var description = element.TryGetProperty("description", out var descProp) ? descProp.GetString() : string.Empty;

            if (string.IsNullOrEmpty(fixtureId))
            {
                return null;
            }

            var channels = new Dictionary<string, byte?>();
            var channelDescriptions = new Dictionary<string, string>();

            if (element.TryGetProperty("channels", out var channelsElement))
            {
                foreach (var channelProp in channelsElement.EnumerateObject())
                {
                    // Check if value is a number or null
                    byte? value = null;
                    if (channelProp.Value.ValueKind == JsonValueKind.Number)
                    {
                        value = (byte)channelProp.Value.GetInt32();
                    }

                    channels[channelProp.Name] = value;
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

            return new Fixture
            {
                Id = fixtureId,
                Name = name ?? fixtureId,
                Manufacturer = manufacturer ?? string.Empty,
                Description = description ?? string.Empty,
                Channels = channels,
                ChannelDescriptions = channelDescriptions,
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRepository: Error parsing fixture element: {ex.Message}");
            return null;
        }
    }

    private async Task SaveFixturesToAppDataAsync(List<Fixture> fixtures)
    {
        try
        {
            var fixturesPath = Path.Combine(_appDataPath, FixturesFileName);

            // Load existing fixtures
            var existing = new List<Fixture>();
            if (File.Exists(fixturesPath))
            {
                var existingJson = await File.ReadAllTextAsync(fixturesPath);
                existing = ParseFixturesFromJson(existingJson);
            }

            // Merge with new fixtures (replace duplicates)
            var mergedDict = existing.ToDictionary(f => f.Id);
            foreach (var fixture in fixtures)
            {
                mergedDict[fixture.Id] = fixture;
            }

            var mergedFixtures = mergedDict.Values.ToList();

            // Convert to JSON
            var fixturesData = new
            {
                fixtures = mergedFixtures.Select(f => new
                {
                    fixtureId = f.Id,
                    name = f.Name,
                    manufacturer = f.Manufacturer,
                    description = f.Description,
                    channels = f.Channels.ToDictionary(kvp => kvp.Key, kvp => (int?)kvp.Value),
                    channelDescriptions = f.ChannelDescriptions,
                }),
            };

            var json = JsonSerializer.Serialize(fixturesData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(fixturesPath, json);

            Debug.WriteLine($"[DEBUG] FixtureRepository: Saved {mergedFixtures.Count} fixtures to AppData");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DEBUG] FixtureRepository: Error saving fixtures: {ex.Message}");
        }
    }
}
