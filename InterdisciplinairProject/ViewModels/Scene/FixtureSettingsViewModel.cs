using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the fixture settings view.
/// </summary>
public class FixtureSettingsViewModel : INotifyPropertyChanged
{
    private const int DebounceDelayMs = 50; // Debounce delay in milliseconds

    private readonly IHardwareConnection _hardwareConnection;
    private readonly Dictionary<string, CancellationTokenSource> _debounceTokens = new();
    private readonly object _debounceLock = new();

    private Fixture? _currentFixture;

    // NIEUW: Houdt de laatst opgeslagen waarden bij voor de Cancel-functionaliteit.
    private Dictionary<string, byte?> _initialChannelValues = new();

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
    /// Gets the DMX start address of the current fixture.
    /// </summary>
    public int StartAddress => _currentFixture?.StartAddress ?? 1;

    /// <summary>
    /// Gets the DMX end address of the current fixture.
    /// </summary>
    public int EndAddress => _currentFixture != null ?
        _currentFixture.StartAddress + _currentFixture.ChannelCount - 1 : 1;

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

        // WIJZIGING: Bewaar een kopie van de oorspronkelijke waarden (de 'opgeslagen' staat).
        // Use GroupBy to handle potential duplicate channel names - take the first occurrence
        _initialChannelValues = new Dictionary<string, byte?>(
            fixture.Channels
                .GroupBy(c => c.Name)
                .Select(g => g.First())
                .ToDictionary(c => c.Name, c => (byte?)c.Parameter));

        // Log warning if duplicates were found
        var duplicates = fixture.Channels
            .GroupBy(c => c.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            Debug.WriteLine($"[WARNING] Fixture '{fixture.Name}' has duplicate channel names: {string.Join(", ", duplicates)}");
        }

        LoadChannelsFromFixture(fixture);
        OnPropertyChanged(nameof(FixtureName));
        OnPropertyChanged(nameof(CurrentFixture));
        OnPropertyChanged(nameof(StartAddress));
        OnPropertyChanged(nameof(EndAddress));
    }

    /// <summary>
    /// Gets the current channel values from the fixture (from the temporary slider state).
    /// </summary>
    /// <returns>A dictionary of channel names and their values.</returns>
    public Dictionary<string, byte?> GetCurrentChannelValues()
    {
        if (_currentFixture == null)
        {
            return new Dictionary<string, byte?>();
        }

        // Update fixture channels met de huidige slider waardes (deze waarden zijn de TIJDELIJKE 'werk'-waarden)
        foreach (var channelVm in Channels)
        {
            var channel = _currentFixture.Channels.FirstOrDefault(c => c.Name == channelVm.Name);
            if (channel != null)
            {
                channel.Parameter = channelVm.Value;
            }
        }

        return _currentFixture.Channels.ToDictionary(c => c.Name, c => (byte?)c.Parameter);
    }

    /// <summary>
    /// NIEUW: Resets the Channel ViewModels and the current fixture's channels to the initial loaded values.
    /// </summary>
    public void CancelChanges()
    {
        if (_currentFixture == null)
        {
            return;
        }

        Debug.WriteLine($"[DEBUG] CancelChanges called. Restoring initial values for {_currentFixture.Name}");

        // 1. Herstel de _currentFixture.Channels naar de oorspronkelijke (opgeslagen) waarden
        _currentFixture.Channels = new ObservableCollection<Channel>(_initialChannelValues.Select(kvp => new Channel
        {
            Name = kvp.Key,
            Value = kvp.Value?.ToString() ?? "0",
            Parameter = kvp.Value ?? 0,
            Type = _currentFixture.ChannelTypes.TryGetValue(kvp.Key, out var ct) ? ct.ToString() : "Unknown",
            Min = 0,
            Max = 255,
            Time = 0,
            ChannelEffect = new ChannelEffect(),
        }));

        // 2. Herlaad de Channel ViewModels om de sliders te updaten (dit stuurt ook de herstelde waarden live naar de hardware)
        LoadChannelsFromFixture(_currentFixture);
    }

    /// <summary>
    /// NIEUW: Confirms the current Channel values as the new initial state (the new 'saved' state).
    /// </summary>
    public void ConfirmSave()
    {
        if (_currentFixture == null)
        {
            return;
        }

        // Zorg ervoor dat de _currentFixture de meest recente sliderwaarden heeft
        GetCurrentChannelValues();

        // Maak een nieuwe kopie van de HUIDIGE waarden van de _currentFixture en stel deze in als de nieuwe 'initial state'
        _initialChannelValues = new Dictionary<string, byte?>(_currentFixture.Channels.ToDictionary(c => c.Name, c => (byte?)c.Parameter));

        Debug.WriteLine($"[DEBUG] ConfirmSave called. New initial values stored for {_currentFixture.Name}");
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
            var type = fixture.ChannelTypes.TryGetValue(channel.Name, out var channelType) ? channelType : ChannelType.Unknown;
            var channelVm = new ChannelViewModel(channel.Name, (byte)channel.Parameter, type);
            Debug.WriteLine($"[DEBUG] Created ChannelViewModel for channel: {channel.Name} = {channel.Parameter}");

            // Subscribe to channel value changes
            channelVm.PropertyChanged += ChannelViewModel_PropertyChanged;

            Channels.Add(channelVm);
        }

        Debug.WriteLine($"[DEBUG] LoadChannelsFromFixture complete. Total channels loaded: {Channels.Count}");
    }

    /// <summary>
    /// Handles channel value changes and sends updates to the DMX hardware in real-time.
    /// Uses debouncing to prevent UI lag during rapid slider movements.
    /// This method is responsible for the LIVE DMX update flow:
    /// 1. User adjusts a slider in the UI.
    /// 2. Channel value is updated in the fixture model immediately (for UI responsiveness).
    /// 3. After a short debounce delay, hardware connection sends the DMX update.
    /// 4. Multiple rapid changes are coalesced into a single DMX update.
    /// </summary>
    /// <param name="sender">The channel view model that changed.</param>
    /// <param name="e">The property changed event args.</param>
    private async void ChannelViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChannelViewModel.Value) && sender is ChannelViewModel channelVm)
        {
            Debug.WriteLine($"[DEBUG] Channel value changed: {channelVm.Name} = {channelVm.Value}");

            // Update the fixture model immediately (for UI responsiveness)
            if (_currentFixture != null)
            {
                var channel = _currentFixture.Channels.FirstOrDefault(c => c.Name == channelVm.Name);
                if (channel != null)
                {
                    channel.Parameter = channelVm.Value;
                }

                Debug.WriteLine($"[DEBUG] Updated fixture model: {_currentFixture.InstanceId}.{channelVm.Name} = {channelVm.Value}");

                // Debounce the DMX hardware update
                await DebouncedSendToHardwareAsync(channelVm.Name, channelVm.Value);
            }
        }
    }

    /// <summary>
    /// Sends the channel value to hardware with debouncing.
    /// Cancels any pending update for the same channel and schedules a new one.
    /// </summary>
    /// <param name="channelName">The name of the channel.</param>
    /// <param name="value">The value to send.</param>
    private async Task DebouncedSendToHardwareAsync(string channelName, byte value)
    {
        CancellationTokenSource newCts;

        lock (_debounceLock)
        {
            // Cancel any existing pending update for this channel
            if (_debounceTokens.TryGetValue(channelName, out var existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }

            // Create a new cancellation token for this update
            newCts = new CancellationTokenSource();
            _debounceTokens[channelName] = newCts;
        }

        try
        {
            // Wait for the debounce delay
            await Task.Delay(DebounceDelayMs, newCts.Token);

            // If we get here, the delay completed without cancellation
            // Send the update to hardware
            if (_currentFixture != null)
            {
                var result = await _hardwareConnection.SetChannelValueAsync(
                    _currentFixture.InstanceId,
                    channelName,
                    value);

                Debug.WriteLine($"[DEBUG] SetChannelValueAsync returned: {result}");
            }
        }
        catch (TaskCanceledException)
        {
            // This is expected when a newer value comes in before the delay completes
            Debug.WriteLine($"[DEBUG] Debounced update cancelled for {channelName} (newer value pending)");
        }
        finally
        {
            // Clean up the token if it's still the current one
            lock (_debounceLock)
            {
                if (_debounceTokens.TryGetValue(channelName, out var currentCts) && currentCts == newCts)
                {
                    _debounceTokens.Remove(channelName);
                    newCts.Dispose();
                }
            }
        }
    }
}