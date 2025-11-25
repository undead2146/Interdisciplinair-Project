using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Interface for managing the fixture registry.
/// The registry maintains a collection of available fixture instances that users can add to scenes.
/// </summary>
public interface IFixtureRegistry
{
    /// <summary>
    /// Gets all fixture instances available in the registry.
    /// </summary>
    /// <returns>A list of all registered fixture instances.</returns>
    Task<List<Fixture>> GetAllFixturesAsync();

    /// <summary>
    /// Gets a fixture instance by its instance ID.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the fixture instance.</param>
    /// <returns>The fixture instance if found; otherwise, null.</returns>
    Fixture? GetFixtureById(string instanceId);

    /// <summary>
    /// Imports fixture instances from a JSON file and adds them to the registry.
    /// </summary>
    /// <param name="filePath">The path to the JSON file containing fixture instances.</param>
    /// <returns>The number of fixtures successfully imported.</returns>
    Task<int> ImportFixturesAsync(string filePath);

    /// <summary>
    /// Adds a fixture instance to the registry.
    /// </summary>
    /// <param name="fixture">The fixture instance to add.</param>
    /// <returns>True if the fixture was added successfully; false if it already exists.</returns>
    Task<bool> AddFixtureAsync(Fixture fixture);

    /// <summary>
    /// Removes a fixture instance from the registry.
    /// </summary>
    /// <param name="instanceId">The ID of the fixture instance to remove.</param>
    /// <returns>True if the fixture was removed successfully; otherwise, false.</returns>
    Task<bool> RemoveFixtureAsync(string instanceId);

    /// <summary>
    /// Creates a new fixture instance from a fixture definition.
    /// </summary>
    /// <param name="fixtureId">The ID of the fixture definition.</param>
    /// <param name="instanceId">The unique ID for the new fixture instance.</param>
    /// <param name="startAddress">The DMX start address for the fixture instance.</param>
    /// <returns>A new fixture instance, or null if the definition was not found.</returns>
    Fixture? CreateFixtureInstance(string fixtureId, string instanceId, int startAddress);

    /// <summary>
    /// Refreshes the registry by reloading all fixture instances from storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshRegistryAsync();

    /// <summary>
    /// Checks if a fixture instance exists in the registry.
    /// </summary>
    /// <param name="instanceId">The ID of the fixture instance to check.</param>
    /// <returns>True if the fixture instance exists; otherwise, false.</returns>
    bool FixtureExists(string instanceId);

    /// <summary>
    /// Exports all fixture instances to a JSON file.
    /// </summary>
    /// <param name="filePath">The path to save the JSON file.</param>
    /// <returns>True if export was successful; otherwise, false.</returns>
    Task<bool> ExportFixturesAsync(string filePath);
}
