using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Core.Repositories;

/// <summary>
/// Repository for fixture data loaded from JSON.
/// </summary>
public class FixtureRepository : IFixtureRepository
{
    private readonly string _fixturesFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRepository"/> class.
    /// </summary>
    /// <param name="fixturesFilePath">The path to the fixtures JSON file.</param>
    public FixtureRepository(string fixturesFilePath)
    {
        _fixturesFilePath = fixturesFilePath;
    }

    /// <summary>
    /// Gets all fixtures from the JSON file.
    /// </summary>
    /// <returns>A list of fixtures.</returns>
    public async Task<List<Fixture>> GetAllFixturesAsync()
    {
        if (!File.Exists(_fixturesFilePath))
        {
            return new List<Fixture>();
        }

        var json = await File.ReadAllTextAsync(_fixturesFilePath);
        var data = JsonSerializer.Deserialize<FixturesData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return data?.Fixtures ?? new List<Fixture>();
    }

    private class FixturesData
    {
        public List<Fixture> Fixtures { get; set; } = new();
    }
}
