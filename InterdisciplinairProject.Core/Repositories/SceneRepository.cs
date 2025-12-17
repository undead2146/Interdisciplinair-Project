using System;
using InterdisciplinairProject.Core.Models;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Repositories;

/// <summary>
/// Repository for scene data stored in JSON.
/// </summary>
public class SceneRepository : ISceneRepository
{
    private readonly string _scenesFilePath;
    private List<Scene> _scenes = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneRepository"/> class.
    /// </summary>
    /// <param name="scenesFilePath">The path to the scenes JSON file.</param>
    public SceneRepository(string scenesFilePath)
    {
        _scenesFilePath = scenesFilePath;
        LoadScenesSync();
    }

    /// <summary>
    /// Gets all scenes.
    /// </summary>
    /// <returns>A list of scenes.</returns>
    public async Task<List<Scene>> GetAllScenesAsync()
    {
        return await Task.FromResult(_scenes.ToList());
    }

    /// <summary>
    /// Gets a scene by ID.
    /// </summary>
    /// <param name="id">The scene ID.</param>
    /// <returns>The scene or null.</returns>
    public async Task<Scene?> GetSceneByIdAsync(string id)
    {
        return await Task.FromResult(_scenes.FirstOrDefault(s => s.Id == id));
    }

    /// <summary>
    /// Saves a scene.
    /// </summary>
    /// <param name="scene">The scene to save.</param>
    /// <returns>A task.</returns>
    public async Task SaveSceneAsync(Scene scene)
    {
        var existing = _scenes.FirstOrDefault(s => s.Id == scene.Id);
        if (existing != null)
        {
            _scenes.Remove(existing);
        }

        _scenes.Add(scene);
        await SaveToFileAsync();
    }

    /// <summary>
    /// Deletes a scene by ID.
    /// </summary>
    /// <param name="id">The scene ID.</param>
    /// <returns>A task.</returns>
    public async Task DeleteSceneAsync(string id)
    {
        var scene = _scenes.FirstOrDefault(s => s.Id == id);
        if (scene != null)
        {
            _scenes.Remove(scene);
            await SaveToFileAsync();
        }
    } 

    /// <summary>
    /// Removes a fixture from a scene.
    /// </summary>
    /// <param name="sceneId">The scene ID.</param>
    /// <param name="fixture">The fixture to remove.</param>
    /// <returns>A task.</returns>
    public async Task RemoveFixtureAsync(string sceneId, Fixture? fixture)
    {
        if (fixture == null)
        {
            System.Diagnostics.Debug.WriteLine($"[WARNING] Fixture is null, cannot remove.");
            return;
        }

        var scene = _scenes.FirstOrDefault(s => s.Id == sceneId);

        if (scene == null)
        {
            System.Diagnostics.Debug.WriteLine($"[WARNING] Scene with ID '{sceneId}' not found.");
            return;
        }

        if (scene.Fixtures == null)
        {
            System.Diagnostics.Debug.WriteLine($"[WARNING] Scene '{scene.Name}' has no fixtures.");
            return;
        }

        var fixtureToRemove = scene.Fixtures.FirstOrDefault(f => f.Id == fixture.Id);

        if (fixtureToRemove != null)
        {
            scene.Fixtures.Remove(fixtureToRemove);
            await SaveToFileAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Fixture '{fixture.Name}' removed from scene '{scene.Name}'.");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[WARNING] Fixture '{fixture.Name}' not found in scene '{scene.Name}'.");
        }
    }

    /// <summary>
    /// Deletes all scenes.
    /// </summary>
    /// <returns>A task.</returns>
    public async Task DeleteAllScenesAsync()
    {
        _scenes.Clear();
        await SaveToFileAsync();
        System.Diagnostics.Debug.WriteLine("[DEBUG] SceneRepository: All scenes deleted.");
    }

    /// <summary>
    /// Validates and imports scenes from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>A tuple with imported scenes and any validation errors.</returns>
    public async Task<(List<Scene> ImportedScenes, string? Error)> ImportScenesFromFileAsync(string filePath)
    {
        var importedScenes = new List<Scene>();

        try
        {
            if (!File.Exists(filePath))
            {
                return (importedScenes, $"Bestand niet gevonden: {filePath}");
            }

            var json = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                return (importedScenes, "Bestand is leeg.");
            }

            // Try to parse as array first, then as single object
            List<Scene>? scenesToImport = null;

            try
            {
                // First try to parse as array
                scenesToImport = JsonSerializer.Deserialize<List<Scene>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                // If that fails, try to parse as single scene
                try
                {
                    var singleScene = JsonSerializer.Deserialize<Scene>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (singleScene != null)
                    {
                        scenesToImport = new List<Scene> { singleScene };
                    }
                }
                catch (JsonException)
                {
                    return (importedScenes, "Dit formaat komt niet overeen met het verwachte scene formaat. Verwacht formaat: JSON array of object met 'id', 'name', 'dimmer', 'fadeInMs', 'fadeOutMs', en 'fixtures' velden.");
                }
            }

            if (scenesToImport == null || scenesToImport.Count == 0)
            {
                return (importedScenes, "Geen geldige scenes gevonden in het bestand.");
            }

            // Validate each scene
            foreach (var scene in scenesToImport)
            {
                var validationError = ValidateSceneFormat(scene);
                if (validationError != null)
                {
                    return (importedScenes, validationError);
                }

                // Ensure unique ID
                if (string.IsNullOrEmpty(scene.Id) || _scenes.Any(s => s.Id == scene.Id))
                {
                    scene.Id = Guid.NewGuid().ToString();
                }

                // Add to repository
                _scenes.Add(scene);
                importedScenes.Add(scene);
            }

            await SaveToFileAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SceneRepository: Imported {importedScenes.Count} scenes from {filePath}");

            return (importedScenes, null);
        }
        catch (JsonException ex)
        {
            return (importedScenes, $"Dit formaat komt niet overeen met het verwachte scene formaat. Details: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (importedScenes, $"Fout bij importeren: {ex.Message}");
        }
    }

    private static string? ValidateSceneFormat(Scene scene)
    {
        if (scene == null)
        {
            return "Scene object is null.";
        }

        if (string.IsNullOrWhiteSpace(scene.Name))
        {
            return "Dit formaat komt niet overeen: 'name' veld ontbreekt of is leeg.";
        }

        // Fixtures can be null or empty, that's ok
        // Dimmer, FadeInMs, FadeOutMs have default values, so they're ok

        return null;
    }

    /// <summary>
    /// Exports all scenes to a JSON file.
    /// </summary>
    /// <param name="filePath">The path to save the JSON file.</param>
    /// <returns>A task representing the operation, with an error message if failed.</returns>
    public async Task<string?> ExportScenesToFileAsync(string filePath)
    {
        try
        {
            if (_scenes.Count == 0)
            {
                return "Er zijn geen scenes om te exporteren.";
            }

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_scenes, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            System.Diagnostics.Debug.WriteLine($"[DEBUG] SceneRepository: Exported {_scenes.Count} scenes to {filePath}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] SceneRepository: Error exporting scenes: {ex.Message}");
            return $"Fout bij exporteren: {ex.Message}";
        }
    }

    private void LoadScenesSync()
    {
        try
        {
            if (!File.Exists(_scenesFilePath))
            {
                return;
            }

            var json = File.ReadAllText(_scenesFilePath);
            _scenes = JsonSerializer.Deserialize<List<Scene>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Scene>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Loading scenes failed: {ex.Message}");
            _scenes = new List<Scene>();
        }
    }

    private async Task SaveToFileAsync()
    {
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_scenesFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SceneRepository: Created directory: {directory}");
            }

            var json = JsonSerializer.Serialize(_scenes, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_scenesFilePath, json);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SceneRepository: Saved {_scenes.Count} scenes to {_scenesFilePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] SceneRepository: Error saving scenes: {ex.Message}");
            throw;
        }
    }
}