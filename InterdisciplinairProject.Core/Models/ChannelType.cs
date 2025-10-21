namespace InterdisciplinairProject.Core.Models;

/// <summary>
/// Represents the type of a DMX channel.
/// </summary>
public enum ChannelType
{
    /// <summary>
    /// Unknown or unspecified channel type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Dimmer/intensity channel.
    /// </summary>
    Dimmer,

    /// <summary>
    /// Red color channel.
    /// </summary>
    Red,

    /// <summary>
    /// Green color channel.
    /// </summary>
    Green,

    /// <summary>
    /// Blue color channel.
    /// </summary>
    Blue,

    /// <summary>
    /// Strobe/flash effect channel.
    /// </summary>
    Strobe,

    /// <summary>
    /// Pan (horizontal movement) channel.
    /// </summary>
    Pan,

    /// <summary>
    /// Tilt (vertical movement) channel.
    /// </summary>
    Tilt,
}
