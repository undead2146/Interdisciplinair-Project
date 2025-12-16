using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models.Enums;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for a single DMX channel with its name and value.
/// </summary>
public class ChannelViewModel : INotifyPropertyChanged
{
    private byte _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelViewModel"/> class.
    /// </summary>
    /// <param name="name">The name of the channel.</param>
    /// <param name="value">The initial value of the channel.</param>
    /// <param name="type">The type of the channel, if known.</param>
    /// <param name="currentEffects">The current effects applied to the channel.</param>
    public ChannelViewModel(string name, byte value, ChannelType? type = null, List<ChannelEffect>? currentEffects = null)
    {
        Name = name;
        _value = value;
        Type = type ?? ChannelTypeHelper.GetChannelTypeFromName(name);
        Symbol = ChannelTypeHelper.GetSymbol(Type);
        ColorHex = ChannelTypeHelper.GetColorHex(Type);

        // Initialize effect options
        EffectOptions = new ObservableCollection<EffectSelectionViewModel>();
        var allEffects = Enum.GetValues<EffectType>();

        foreach (var effectType in allEffects)
        {
            // Check if this effect is enabled in the passed list
            bool isEnabled = currentEffects?.Any(e => e.EffectType == effectType && e.Enabled) == true;
            EffectOptions.Add(new EffectSelectionViewModel(effectType, isEnabled));
        }

        Debug.WriteLine($"[DEBUG] ChannelViewModel created: {Name}, Effects loaded: {EffectOptions.Count(e => e.IsSelected)}");
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the name of the channel.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the channel.
    /// </summary>
    public ChannelType Type { get; }

    /// <summary>
    /// Gets the symbol/icon for this channel type.
    /// </summary>
    public string Symbol { get; }

    /// <summary>
    /// Gets the color hex string for this channel type.
    /// </summary>
    public string ColorHex { get; }

    /// <summary>
    /// Gets or sets the value of the channel (0-255).
    /// </summary>
    public byte Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                Debug.WriteLine($"[DEBUG] ChannelViewModel.Value setter called for {Name}: {_value} -> {value}");
                _value = value;
                OnPropertyChanged(nameof(Value));
                Debug.WriteLine($"[DEBUG] ChannelViewModel.Value PropertyChanged raised for {Name} = {value}");
            }
            else
            {
                Debug.WriteLine($"[DEBUG] ChannelViewModel.Value setter called for {Name} but value unchanged: {value}");
            }
        }
    }

    /// <summary>
    /// Gets the collection of available effect options for multi-selection.
    /// </summary>
    public ObservableCollection<EffectSelectionViewModel> EffectOptions { get; }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
