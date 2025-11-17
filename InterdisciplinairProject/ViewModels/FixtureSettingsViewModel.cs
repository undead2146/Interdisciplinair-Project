using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the fixture settings view.
/// </summary>
public class FixtureSettingsViewModel : INotifyPropertyChanged
{
    private readonly IHardwareConnection _hardwareConnection;
    private Fixture? _currentFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureSettingsViewModel"/> class.
    /// </summary>
    /// <param name="hardwareConnection">The hardware connection service.</param>
    public FixtureSettingsViewModel(IHardwareConnection hardwareConnection)
    {
        _hardwareConnection = hardwareConnection;
        Channels = new ObservableCollection<ChannelViewModel>();
    }

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the collection of channel view models.
    /// </summary>
    public ObservableCollection<ChannelViewModel> Channels { get; }

    /// <summary>
    /// Gets the current fixture.
    /// </summary>
    public Fixture? CurrentFixture => _currentFixture;

    /// <summary>
    /// Gets the name of the current fixture.
    /// </summary>
    public string FixtureName => _currentFixture?.Name ?? "Selecteer een fixture";

    /// <summary>
    /// Loads a new fixture into the view model.
    /// </summary>
    /// <param name="fixture">The fixture to load.</param>
    public void LoadFixture(Fixture fixture)
    {
        if (fixture == null)
        {
            Debug.WriteLine("[DEBUG] LoadFixture called with null fixture");
            return;
        }

        Debug.WriteLine($"[DEBUG] LoadFixture called for: {fixture.Name}");
        _currentFixture = fixture;
        LoadChannelsFromFixture(fixture);
        OnPropertyChanged(nameof(FixtureName));
        OnPropertyChanged(nameof(CurrentFixture));
    }

    /// <summary>
    /// Gets the current channel values from the fixture.
    /// </summary>
    /// <returns>A dictionary of channel names and their values.</returns>
    public Dictionary<string, byte?> GetCurrentChannelValues()
    {
        if (_currentFixture == null)
        {
            return new Dictionary<string, byte?>();
        }

        // Update fixture channels met de huidige slider waardes
        foreach (var channelVm in Channels)
        {
            _currentFixture.Channels[channelVm.Name] = channelVm.Value;
        }

        return _currentFixture.Channels;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void LoadChannelsFromFixture(Fixture fixture)
    {
        Debug.WriteLine($"[DEBUG] LoadChannelsFromFixture called for fixture: {fixture.Name}");

        // Unsubscribe van oude channels
        foreach (var channel in Channels)
        {
            channel.PropertyChanged -= ChannelViewModel_PropertyChanged;
        }

        Channels.Clear();

        foreach (var channel in fixture.Channels)
        {
            var type = fixture.ChannelTypes.TryGetValue(channel.Key, out var channelType) ? channelType : ChannelType.Unknown;
            var channelVm = new ChannelViewModel(channel.Key, channel.Value ?? 0, type);
            Debug.WriteLine($"[DEBUG] Created ChannelViewModel for channel: {channel.Key} = {channel.Value ?? 0}");

            // Subscribe to channel value changes
            channelVm.PropertyChanged += ChannelViewModel_PropertyChanged;

            Channels.Add(channelVm);
        }

        Debug.WriteLine($"[DEBUG] LoadChannelsFromFixture complete. Total channels loaded: {Channels.Count}");
    }

    private async void ChannelViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChannelViewModel.Value) && sender is ChannelViewModel channelVm)
        {
            Debug.WriteLine($"[DEBUG] Channel value changed: {channelVm.Name} = {channelVm.Value}");

            // Update the fixture model
            if (_currentFixture != null)
            {
                _currentFixture.Channels[channelVm.Name] = channelVm.Value;
                Debug.WriteLine($"[DEBUG] Updated fixture model: {_currentFixture.InstanceId}.{channelVm.Name} = {channelVm.Value}");

                // Send to hardware connection
                var result = await _hardwareConnection.SetChannelValueAsync(
                    _currentFixture.InstanceId,
                    channelVm.Name,
                    channelVm.Value);

                Debug.WriteLine($"[DEBUG] SetChannelValueAsync returned: {result}");
            }
        }
    }
}
