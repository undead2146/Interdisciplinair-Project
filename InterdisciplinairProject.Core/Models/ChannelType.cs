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
    /// White color channel.
    /// </summary>
    White,

    /// <summary>
    /// Amber color channel.
    /// </summary>
    Amber,

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

    /// <summary>
    /// Color temperature channel.
    /// </summary>
    ColorTemperature,

    /// <summary>
    /// Gobo/pattern selection channel.
    /// </summary>
    Gobo,

    /// <summary>
    /// Color wheel channel.
    /// </summary>
    Color,

    /// <summary>
    /// Speed control channel.
    /// </summary>
    Speed,

    /// <summary>
    /// Pattern selection channel.
    /// </summary>
    Pattern,

    /// <summary>
    /// Power/intensity channel.
    /// </summary>
    Power,

    /// <summary>
    /// Rate control channel.
    /// </summary>
    Rate,

    /// <summary>
    /// Brightness channel.
    /// </summary>
    Brightness,
}
