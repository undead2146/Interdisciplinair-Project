using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models.Enums;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace InterdisciplinairProject.ViewModels.Scene;

/// <summary>
/// Represents an effect row for channel configuration.
/// </summary>
public class EffectRow : INotifyPropertyChanged
{
    // De statische EffectMapping is niet langer nodig voor het filteren, maar we laten de structuur intact.
    private static readonly Dictionary<string, List<EffectType>> EffectMapping =
        new Dictionary<string, List<EffectType>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Dimmer", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse, EffectType.Strobe } },
            { "Intensity", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse, EffectType.Strobe } },
            { "Red", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
            { "Green", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
            { "Blue", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
            { "White", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
            { "Amber", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
            { "Strobe", new List<EffectType> { EffectType.Strobe, EffectType.Pulse } },
            { "Pan", new List<EffectType> { EffectType.Custom } },
            { "Tilt", new List<EffectType> { EffectType.Custom } },
            { "Gobo", new List<EffectType> { EffectType.Custom } },
            { "Color", new List<EffectType> { EffectType.Custom } },
            { "Speed", new List<EffectType> { EffectType.Custom } },
            { "Lamp", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Strobe, EffectType.Pulse } },
            { "Star", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
            { "Custom", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Strobe, EffectType.Pulse, EffectType.Custom } },
        };

    // Holds all channels from the fixture template
    private readonly IEnumerable<Channel> _allChannels;

    // Now represents the single channel selected from the filtered list
    private Channel? _selectedChannel;

    private EffectType _effectType = EffectType.FadeIn;
    private ObservableCollection<Channel>? _availableChannels; // List of channels filtered by the selected effect

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectRow"/> class.
    /// </summary>
    /// <param name="allChannels">All channels.</param>
    public EffectRow(IEnumerable<Channel> allChannels)
    {
        _allChannels = allChannels;

        // Initial update based on default EffectType.FadeIn
        UpdateAvailableChannels();
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the effect type.
    /// </summary>
    public EffectType EffectType
    {
        get => _effectType;
        set
        {
            if (_effectType != value)
            {
                _effectType = value;
                OnPropertyChanged(nameof(EffectType));
                UpdateAvailableChannels(); // <-- Trigger channel filtering when effect changes
            }
        }
    }

    /// <summary>
    /// Gets or sets the min value.
    /// </summary>
    public string Min { get; set; } = "0";

    /// <summary>
    /// Gets or sets the max value.
    /// </summary>
    public string Max { get; set; } = "255";

    /// <summary>
    /// Gets or sets the time.
    /// </summary>
    public string Time { get; set; } = "1000";

    // Property for the selected channel

    /// <summary>
    /// Gets or sets the selected channel.
    /// </summary>
    public Channel? SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            if (_selectedChannel != value)
            {
                _selectedChannel = value;
                OnPropertyChanged(nameof(SelectedChannel));

                // Update effect parameters if a channel is selected and it has an existing effect
                if (_selectedChannel?.ChannelEffect != null)
                {
                    Min = _selectedChannel.ChannelEffect.Min.ToString();
                    Max = _selectedChannel.ChannelEffect.Max.ToString();
                    Time = _selectedChannel.ChannelEffect.Time.ToString();
                    OnPropertyChanged(nameof(Min));
                    OnPropertyChanged(nameof(Max));
                    OnPropertyChanged(nameof(Time));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the available channels, filtered by the currently selected EffectType.
    /// </summary>
    public ObservableCollection<Channel> AvailableChannels
    {
        get => _availableChannels ??= new ObservableCollection<Channel>();
        set
        {
            _availableChannels = value;
            OnPropertyChanged(nameof(AvailableChannels));
        }
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Gets the appropriate effects for a channel type (Still defined, but not used for filtering in this mode).
    /// </summary>
    private static List<EffectType> GetEffectsForChannelType(string channelType)
    {
        if (EffectMapping.TryGetValue(channelType, out var effects))
        {
            return effects;
        }

        return Enum.GetValues(typeof(EffectType)).Cast<EffectType>().ToList();
    }

    // DE BELANGRIJKSTE WIJZIGING: Filtert op basis van bestaande JSON-waarde
    private void UpdateAvailableChannels()
    {
        var filteredChannels = _allChannels
            .Where(channel =>
            {
                // Controleer of het kanaal een bestaand effect heeft.
                if (channel.ChannelEffect != null)
                {
                    // Retourneer ALLEEN het kanaal waarvan het bestaande effecttype
                    // overeenkomt met het geselecteerde EffectType in de dropdown.
                    return channel.ChannelEffect.EffectType == EffectType;
                }

                // Als het kanaal GEEN bestaand effect heeft, wordt het uitgesloten.
                return false;
            })
            .ToList();

        // Update de ObservableCollection
        AvailableChannels = new ObservableCollection<Channel>(filteredChannels);

        // Re-selecteer het kanaal indien nodig.
        if (SelectedChannel == null || !AvailableChannels.Contains(SelectedChannel))
        {
            SelectedChannel = AvailableChannels.FirstOrDefault();
        }
    }
}