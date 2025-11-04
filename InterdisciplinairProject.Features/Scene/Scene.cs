using System.Collections.Generic;

namespace InterdisciplinairProject.Features.Scene;

/// <summary>
/// Represents a scene that contains fixtures and configuration.
/// </summary>
public class Scene
{
    /// <summary>
    /// Gets or sets the scene identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scene name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DMX universe for this scene.
    /// </summary>
    public int Universe { get; set; } = 1;

    /// <summary>
    /// Gets or sets the fixtures contained in this scene.
    /// </summary>
    public List<SceneFixture> Fixtures { get; set; } = new List<SceneFixture>();
}
