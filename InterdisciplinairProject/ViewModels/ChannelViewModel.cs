using System.ComponentModel;
using System.Diagnostics;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Features.Fixture;

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
    public ChannelViewModel(string name, byte value)
    {
        Name = name;
        _value = value;
        Type = ChannelTypeHelper.GetChannelTypeFromName(name);
        Symbol = ChannelTypeHelper.GetSymbol(Type);
        ColorHex = ChannelTypeHelper.GetColorHex(Type);
        Debug.WriteLine($"[DEBUG] ChannelViewModel created: {Name} = {value}, Type: {Type}, Symbol: {Symbol}, Color: {ColorHex}");
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
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
