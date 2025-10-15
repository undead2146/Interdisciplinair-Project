using System;
using System.Collections.Generic;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a DMX scene.
/// </summary>
public class Scene
{
    /// <summary>
    /// Gets or sets the unique identifier of the scene.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the scene.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation date of the scene.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets the list of scene fixtures in this scene.
    /// </summary>
    public List<SceneFixture> Fixtures { get; set; } = new();
}

/// <summary>
/// Represents a fixture instance in a scene.
/// </summary>
public class SceneFixture
{
    /// <summary>
    /// Gets or sets the unique identifier for this instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the fixture definition.
    /// </summary>
    public Fixture Fixture { get; set; } = new();

    /// <summary>
    /// Gets or sets the DMX universe for this fixture.
    /// </summary>
    public int Universe { get; set; } = 1;

    /// <summary>
    /// Gets or sets the starting DMX channel for this fixture.
    /// </summary>
    public int StartChannel { get; set; } = 1;

    /// <summary>
    /// Gets or sets the channel values for this fixture.
    /// </summary>
    public Dictionary<string, byte> ChannelValues { get; set; } = new();
}
