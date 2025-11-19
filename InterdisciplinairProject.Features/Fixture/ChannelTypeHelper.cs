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
            ChannelType.Red => "🔴",
            ChannelType.Green => "🟢",
            ChannelType.Blue => "🔵",
            ChannelType.White => "⚪",
            ChannelType.Amber => "🟠",
            ChannelType.Strobe => "⚡",
            ChannelType.Pan => "↔",
            ChannelType.Tilt => "↕",
            ChannelType.ColorTemperature => "🌡",
            ChannelType.Gobo => "⭐",
            ChannelType.Color => "🎨",
            ChannelType.Speed => "⏩",
            ChannelType.Pattern => "🔶",
            ChannelType.Power => "🔋",
            ChannelType.Rate => "⏱",
            ChannelType.Brightness => "☀",
            ChannelType.Unknown => "❓",
            _ => "❓",
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
            "intensity" => ChannelType.Dimmer,
            "red" => ChannelType.Red,
            "r" => ChannelType.Red,
            "green" => ChannelType.Green,
            "g" => ChannelType.Green,
            "blue" => ChannelType.Blue,
            "b" => ChannelType.Blue,
            "white" => ChannelType.White,
            "w" => ChannelType.White,
            "amber" => ChannelType.Amber,
            "a" => ChannelType.Amber,
            "strobe" => ChannelType.Strobe,
            "pan" => ChannelType.Pan,
            "tilt" => ChannelType.Tilt,
            "color_temperature" => ChannelType.ColorTemperature,
            "color temperature" => ChannelType.ColorTemperature,
            "cct" => ChannelType.ColorTemperature,
            "gobo" => ChannelType.Gobo,
            "color" => ChannelType.Color,
            "speed" => ChannelType.Speed,
            "pattern" => ChannelType.Pattern,
            "power" => ChannelType.Power,
            "rate" => ChannelType.Rate,
            "brightness" => ChannelType.Brightness,
            // Handle generic channel names like Ch1, Ch2, etc. as Dimmer
            var name when name.StartsWith("ch") && int.TryParse(name[2..], out _) => ChannelType.Dimmer,
            var name when name.StartsWith("channel") && int.TryParse(name[7..], out _) => ChannelType.Dimmer,
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
            ChannelType.White => "White",
            ChannelType.Amber => "Amber",
            ChannelType.Strobe => "Strobe",
            ChannelType.Pan => "Pan",
            ChannelType.Tilt => "Tilt",
            ChannelType.ColorTemperature => "Color Temperature",
            ChannelType.Gobo => "Gobo",
            ChannelType.Color => "Color",
            ChannelType.Speed => "Speed",
            ChannelType.Pattern => "Pattern",
            ChannelType.Power => "Power",
            ChannelType.Rate => "Rate",
            ChannelType.Brightness => "Brightness",
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
            ChannelType.Dimmer => "#FFD700",       // Gold
            ChannelType.Red => "#FF0000",          // Red
            ChannelType.Green => "#00FF00",        // Green
            ChannelType.Blue => "#0000FF",         // Blue
            ChannelType.White => "#FFFFFF",        // White
            ChannelType.Amber => "#FFBF00",        // Amber
            ChannelType.Strobe => "#FFFF00",       // Yellow
            ChannelType.Pan => "#00BFFF",          // Deep Sky Blue
            ChannelType.Tilt => "#FF69B4",         // Hot Pink
            ChannelType.ColorTemperature => "#FFA500", // Orange
            ChannelType.Gobo => "#9370DB",         // Medium Purple
            ChannelType.Color => "#FF1493",        // Deep Pink
            ChannelType.Speed => "#00CED1",        // Dark Turquoise
            ChannelType.Pattern => "#FF8C00",      // Dark Orange
            ChannelType.Power => "#32CD32",        // Lime Green
            ChannelType.Rate => "#4169E1",         // Royal Blue
            ChannelType.Brightness => "#FFD700",   // Gold
            ChannelType.Unknown => "#808080",      // Gray
            _ => "#808080",
        };
    }
}
