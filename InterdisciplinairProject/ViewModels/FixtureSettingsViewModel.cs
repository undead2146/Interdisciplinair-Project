using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Features.Fixture;
using InterdisciplinairProject.Services;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the fixture settings view.
/// </summary>
public class FixtureSettingsViewModel : INotifyPropertyChanged
{
    private readonly IHardwareConnection _hardwareConnection;
    private Fixture _currentFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureSettingsViewModel"/> class.
    /// </summary>
    public FixtureSettingsViewModel()
    {
        Debug.WriteLine("[DEBUG] FixtureSettingsViewModel constructor called");
        _hardwareConnection = new HardwareConnection();
        Debug.WriteLine("[DEBUG] HardwareConnection created");

        // Initialize with demo fixture - in production this would come from a service/repository
        _currentFixture = new Fixture
        {
            FixtureId = "rgb-wash-b",
            Name = "Wash Light B",
            Channels = new Dictionary<string, byte?>
            {
                { "red", 128 },
                { "green", 128 },
                { "blue", 128 },
                { "dimmer", 255 },
                { "strobe", 0 },
                { "tilt", 127 },
                { "pan", 127 },
            },
        };
        Debug.WriteLine($"[DEBUG] Demo fixture created: {_currentFixture.Name} with {_currentFixture.Channels.Count} channels");

        // Create channel view models from the fixture's channels
        Channels = new ObservableCollection<ChannelViewModel>();
        LoadChannelsFromFixture(_currentFixture);
        Debug.WriteLine($"[DEBUG] FixtureSettingsViewModel initialization complete. Channels collection has {Channels.Count} items");
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
    /// Gets the name of the current fixture.
    /// </summary>
    public string FixtureName => _currentFixture?.Name ?? "No Fixture";

    /// <summary>
    /// Loads a new fixture into the view model.
    /// </summary>
    /// <param name="fixture">The fixture to load.</param>
    public void LoadFixture(Fixture fixture)
    {
        if (fixture == null)
        {
            throw new ArgumentNullException(nameof(fixture));
        }

        _currentFixture = fixture;
        LoadChannelsFromFixture(fixture);
        OnPropertyChanged(nameof(FixtureName));
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
        Channels.Clear();
        Debug.WriteLine("[DEBUG] Channels collection cleared");

        foreach (var channel in fixture.Channels)
        {
            var channelVm = new ChannelViewModel(channel.Key, channel.Value ?? 0);
            Debug.WriteLine($"[DEBUG] Created ChannelViewModel for channel: {channel.Key} = {channel.Value ?? 0}");

            // Subscribe to channel value changes
            channelVm.PropertyChanged += ChannelViewModel_PropertyChanged;
            Debug.WriteLine($"[DEBUG] Subscribed to PropertyChanged event for channel: {channel.Key}");

            Channels.Add(channelVm);
            Debug.WriteLine($"[DEBUG] Added ChannelViewModel to Channels collection: {channel.Key}");
        }
        Debug.WriteLine($"[DEBUG] LoadChannelsFromFixture complete. Total channels loaded: {Channels.Count}");
    }

    private async void ChannelViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Debug.WriteLine($"[DEBUG] ChannelViewModel_PropertyChanged called. Property: {e.PropertyName}, Sender: {sender?.GetType().Name}");
        if (e.PropertyName == nameof(ChannelViewModel.Value) && sender is ChannelViewModel channelVm)
        {
            Debug.WriteLine($"[DEBUG] Channel value changed: {channelVm.Name} = {channelVm.Value}");

            // Update the fixture model
            _currentFixture.Channels[channelVm.Name] = channelVm.Value;
            Debug.WriteLine($"[DEBUG] Updated fixture model: {_currentFixture.FixtureId}.{channelVm.Name} = {channelVm.Value}");

            // Send to hardware connection
            Debug.WriteLine($"[DEBUG] About to call hardware connection: {_currentFixture.FixtureId}, {channelVm.Name}, {channelVm.Value}");
            await _hardwareConnection.SetChannelValueAsync(
                _currentFixture.FixtureId,
                channelVm.Name,
                channelVm.Value);
            Debug.WriteLine($"[DEBUG] Hardware connection call completed for {channelVm.Name}");
        }
        else
        {
            Debug.WriteLine($"[DEBUG] ChannelViewModel_PropertyChanged ignored - not a Value change or invalid sender");
        }
    }
}
