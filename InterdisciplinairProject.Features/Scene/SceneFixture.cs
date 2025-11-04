using System.Collections.Generic;

namespace InterdisciplinairProject.Features.Scene;

/// <summary>
/// Represents a fixture instance inside a scene.
/// </summary>
public class SceneFixture
{
    /// <summary>
    /// Gets or sets the fixture identifier.
    /// </summary>
    public string FixtureId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the instance identifier for the fixture.
    /// </summary>
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the instance.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets DMX channel values for this instance.
    /// </summary>
    public Dictionary<string, byte?> Channels { get; set; } = new Dictionary<string, byte?>();
}
