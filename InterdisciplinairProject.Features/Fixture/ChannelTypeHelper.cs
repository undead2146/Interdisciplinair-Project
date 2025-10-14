using System;

namespace InterdisciplinairProject.Features.Fixture;

/// <summary>
/// Helper class for working with channel types.
/// </summary>
public static class ChannelTypeHelper
{
    /// <summary>
    /// Gets the symbol/icon for a channel type.
    /// </summary>
    /// <param name="channelType">The channel type.</param>
    /// <returns>The symbol representing the channel type.</returns>
    public static string GetSymbol(ChannelType channelType)
    {
        return channelType switch
        {
            ChannelType.Dimmer => "💡",
            ChannelType.Red => "R",
            ChannelType.Green => "G",
            ChannelType.Blue => "B",
            ChannelType.Strobe => "⚡",
            ChannelType.Pan => "↔",
            ChannelType.Tilt => "↕",
            ChannelType.Unknown => "?",
            _ => "?",
        };
    }

    /// <summary>
    /// Gets the channel type from a channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel.</param>
    /// <returns>The corresponding channel type.</returns>
    public static ChannelType GetChannelTypeFromName(string channelName)
    {
        if (string.IsNullOrWhiteSpace(channelName))
        {
            return ChannelType.Unknown;
        }

        return channelName.ToLowerInvariant() switch
        {
            "dimmer" => ChannelType.Dimmer,
            "red" => ChannelType.Red,
            "green" => ChannelType.Green,
            "blue" => ChannelType.Blue,
            "strobe" => ChannelType.Strobe,
            "pan" => ChannelType.Pan,
            "tilt" => ChannelType.Tilt,
            _ => ChannelType.Unknown,
        };
    }

    /// <summary>
    /// Gets the display name for a channel type.
    /// </summary>
    /// <param name="channelType">The channel type.</param>
    /// <returns>The display name for the channel type.</returns>
    public static string GetDisplayName(ChannelType channelType)
    {
        return channelType switch
        {
            ChannelType.Dimmer => "Dimmer",
            ChannelType.Red => "Red",
            ChannelType.Green => "Green",
            ChannelType.Blue => "Blue",
            ChannelType.Strobe => "Strobe",
            ChannelType.Pan => "Pan",
            ChannelType.Tilt => "Tilt",
            ChannelType.Unknown => "Unknown",
            _ => "Unknown",
        };
    }

    /// <summary>
    /// Gets the color hex string for a channel type.
    /// </summary>
    /// <param name="channelType">The channel type.</param>
    /// <returns>The hex color string for the channel type.</returns>
    public static string GetColorHex(ChannelType channelType)
    {
        return channelType switch
        {
            ChannelType.Dimmer => "#FFD700",  // Gold
            ChannelType.Red => "#FF0000",     // Red
            ChannelType.Green => "#00FF00",   // Green
            ChannelType.Blue => "#0000FF",    // Blue
            ChannelType.Strobe => "#FFFF00",  // Yellow
            ChannelType.Pan => "#00BFFF",     // Deep Sky Blue
            ChannelType.Tilt => "#FF69B4",    // Hot Pink
            ChannelType.Unknown => "#808080", // Gray
            _ => "#808080",
        };
    }
}
