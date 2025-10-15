using System.Collections.Generic;

namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents a DMX fixture.
/// </summary>
public class Fixture
{
    /// <summary>
    /// Gets or sets the unique identifier of the fixture.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the fixture.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manufacturer of the fixture.
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of channels for this fixture.
    /// </summary>
    public List<Channel> Channels { get; set; } = new();
}

/// <summary>
/// Represents a DMX channel.
/// </summary>
public class Channel
{
    /// <summary>
    /// Gets or sets the name of the channel.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the channel.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default value of the channel.
    /// </summary>
    public byte Default { get; set; }
}
