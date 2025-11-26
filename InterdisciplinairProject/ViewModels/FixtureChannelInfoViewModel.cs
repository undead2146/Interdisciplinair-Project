using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for displaying detailed channel information in the import fixtures dialog.
/// </summary>
public partial class FixtureChannelInfoViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureChannelInfoViewModel"/> class.
    /// </summary>
    /// <param name="channelNumber">The channel number.</param>
    /// <param name="channelName">The channel name.</param>
    /// <param name="description">The channel description.</param>
    public FixtureChannelInfoViewModel(int channelNumber, string channelName, string description)
    {
        ChannelNumber = channelNumber;
        ChannelName = channelName;
        Description = description;
        ChannelType = ChannelTypeHelper.GetChannelTypeFromName(channelName);
        Symbol = ChannelTypeHelper.GetSymbol(ChannelType);
        ColorHex = ChannelTypeHelper.GetColorHex(ChannelType);
        DisplayName = ChannelTypeHelper.GetDisplayName(ChannelType);
    }

    /// <summary>
    /// Gets the channel number.
    /// </summary>
    public int ChannelNumber { get; }

    /// <summary>
    /// Gets the channel name.
    /// </summary>
    public string ChannelName { get; }

    /// <summary>
    /// Gets the channel description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the channel type.
    /// </summary>
    public ChannelType ChannelType { get; }

    /// <summary>
    /// Gets the symbol for the channel type.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the color hex for the channel type.
    /// </summary>
    public string ColorHex { get; }

    /// <summary>
    /// Gets the display name for the channel type.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the formatted channel info (e.g., "Ch1: Dimmer - General intensity").
    /// </summary>
    public string FormattedInfo => $"Ch{ChannelNumber}: {ChannelName}" +
        (string.IsNullOrEmpty(Description) ? string.Empty : $" - {Description}");
}
