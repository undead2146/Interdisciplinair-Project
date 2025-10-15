using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for a single channel setting.
/// </summary>
public partial class ChannelSettingViewModel : ObservableObject
{
    private readonly IHardwareConnection _hardwareConnection;
    private readonly SceneFixture _sceneFixture;
    private readonly Channel _channel;

    [ObservableProperty]
    private string _channelName = string.Empty;

    [ObservableProperty]
    private string _channelType = string.Empty;

    [ObservableProperty]
    private byte _value;

    [ObservableProperty]
    private string _displayValue = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelSettingViewModel"/> class.
    /// </summary>
    /// <param name="hardwareConnection">The hardware connection.</param>
    /// <param name="sceneFixture">The scene fixture.</param>
    /// <param name="channel">The channel.</param>
    public ChannelSettingViewModel(IHardwareConnection hardwareConnection, SceneFixture sceneFixture, Channel channel)
    {
        _hardwareConnection = hardwareConnection;
        _sceneFixture = sceneFixture;
        _channel = channel;
        ChannelName = $"{channel.Name} (U{sceneFixture.Universe}:{sceneFixture.StartChannel + sceneFixture.Fixture.Channels.IndexOf(channel)})";
        ChannelType = channel.Type;
        Value = sceneFixture.ChannelValues.TryGetValue(channel.Name, out var val) ? val : channel.Default;
        UpdateDisplayValue();
    }

    partial void OnValueChanged(byte value)
    {
        _sceneFixture.ChannelValues[_channel.Name] = value;
        UpdateDisplayValue();
        SendToHardware();
    }

    private void UpdateDisplayValue()
    {
        DisplayValue = $"{Value} ({Value * 100 / 255}%)";
    }

    private async void SendToHardware()
    {
        var channelIndex = _sceneFixture.Fixture.Channels.IndexOf(_channel);
        var dmxChannel = _sceneFixture.StartChannel + channelIndex;
        await _hardwareConnection.SendDmxValueAsync(_sceneFixture.Universe, dmxChannel, Value);
    }
}
