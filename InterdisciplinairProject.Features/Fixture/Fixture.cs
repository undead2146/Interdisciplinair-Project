using System.Collections.Generic;

namespace InterdisciplinairProject.Features.Fixture;

/// <summary>
/// Represents a lighting fixture with its properties and channels.
/// </summary>
public class Fixture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Fixture"/> class.
    /// </summary>
    public Fixture()
    {
        FixtureId = string.Empty;
        Name = string.Empty;
        Channels = new Dictionary<string, byte?>();
    }

    /// <summary>
    /// Gets or sets the unique identifier of the fixture.
    /// </summary>
    public string FixtureId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the fixture.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the channels of the fixture with their current values.
    /// </summary>
    public Dictionary<string, byte?> Channels { get; set; }
}
