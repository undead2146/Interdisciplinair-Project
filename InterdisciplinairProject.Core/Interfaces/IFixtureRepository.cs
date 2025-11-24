using InterdisciplinairProject.Core.Models;
using System.Threading.Tasks;

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
    Task<List<FixtureDefinition>> GetAllFixturesAsync();

    /// <summary>
    /// Gets a fixture by its ID.
    /// </summary>
    /// <param name="fixtureId">The fixture ID.</param>
    /// <returns>The fixture, or null if not found.</returns>
    FixtureDefinition? GetFixtureById(string fixtureId);
}
