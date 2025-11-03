using System;
using System.Collections.Generic;
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
        var json = JsonSerializer.Serialize(_scenes, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_scenesFilePath, json);
    }
}
