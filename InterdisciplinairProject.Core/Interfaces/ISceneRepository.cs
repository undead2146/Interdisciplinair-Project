using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Repository interface for scene data.
/// </summary>
public interface ISceneRepository
{
    /// <summary>
    /// Gets all scenes.
    /// </summary>
    /// <returns>A list of scenes.</returns>
    Task<List<Scene>> GetAllScenesAsync();

    /// <summary>
    /// Gets a scene by ID.
    /// </summary>
    /// <param name="id">The scene ID.</param>
    /// <returns>The scene or null.</returns>
    Task<Scene?> GetSceneByIdAsync(string id);

    /// <summary>
    /// Saves a scene.
    /// </summary>
    /// <param name="scene">The scene to save.</param>
    /// <returns>A task.</returns>
    Task SaveSceneAsync(Scene scene);

    /// <summary>
    /// Deletes a scene by ID.
    /// </summary>
    /// <param name="id">The scene ID.</param>
    /// <returns>A task.</returns>
    Task DeleteSceneAsync(string id);

    /// <summary>
    /// Removes a fixture from a scene.
    /// </summary>
    /// <param name="sceneId">The scene ID.</param>
    /// <param name="fixture">The fixture to remove.</param>
    /// <returns>A task.</returns>
    Task RemoveFixtureAsync(string sceneId, Fixture? fixture);

    /// <summary>
    /// Deletes all scenes.
    /// </summary>
    /// <returns>A task.</returns>
    Task DeleteAllScenesAsync();

    /// <summary>
    /// Validates and imports scenes from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>A tuple with imported scenes and any validation errors.</returns>
    Task<(List<Scene> ImportedScenes, string? Error)> ImportScenesFromFileAsync(string filePath);

    /// <summary>
    /// Exports all scenes to a JSON file.
    /// </summary>
    /// <param name="filePath">The path to save the JSON file.</param>
    /// <returns>A task representing the operation, with an error message if failed.</returns>
    Task<string?> ExportScenesToFileAsync(string filePath);
}