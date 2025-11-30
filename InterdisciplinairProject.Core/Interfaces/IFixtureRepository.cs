using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Interfaces;

/// <summary>
/// Repository interface for fixture data.
/// </summary>
public interface IFixtureRepository
{
    /// <summary>
    /// Gets all available fixtures.
    /// </summary>
    /// <returns>A list of fixtures.</returns>
    Task<List<Fixture>> GetAllFixturesAsync();

    /// <summary>
    /// Gets a fixture by its ID.
    /// </summary>
    /// <param name="fixtureId">The fixture ID.</param>
    /// <returns>The fixture, or null if not found.</returns>
    Fixture? GetFixtureById(string fixtureId);
}
