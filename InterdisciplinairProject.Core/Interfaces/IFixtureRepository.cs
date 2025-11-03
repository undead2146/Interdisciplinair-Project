using System.Collections.Generic;
using System.Threading.Tasks;
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
}
