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
    private readonly IHardwareConnection _hardwareConnection;
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

        // NIEUW: Bereken de verhoudingen tussen de channels op basis van de JSON waarden
        _currentFixture.CalculateChannelRatios();

        // WIJZIGING: Bewaar een kopie van de oorspronkelijke waarden (de 'opgeslagen' staat).
        _initialChannelValues = new Dictionary<string, byte?>(fixture.Channels.ToDictionary(c => c.Name, c => (byte?)c.Parameter));

        // Calculate initial dimmer percentage
        _dimmerPercentage = (int)Math.Round(_currentFixture.Dimmer * 100.0 / 255.0);

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

        // 2. Recalculeer de verhoudingen op basis van de herstelde waarden
        _currentFixture.CalculateChannelRatios();
        _dimmerPercentage = (int)Math.Round(_currentFixture.Dimmer * 100.0 / 255.0);
        OnPropertyChanged(nameof(DimmerPercentage));

        // 3. Herlaad de Channel ViewModels om de sliders te updaten (dit stuurt ook de herstelde waarden live naar de hardware)
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

        // Herbereken verhoudingen op basis van de huidige waarden
        _currentFixture.CalculateChannelRatios();

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
    /// Updates the channel ViewModels from the current fixture's channel values without triggering change events.
    /// </summary>
    private void UpdateChannelViewModelsFromFixture()
    {
        if (_currentFixture == null)
        {
            return;
        }

        foreach (var channelVm in Channels)
        {
            // Unsubscribe temporarily to avoid triggering hardware updates
            channelVm.PropertyChanged -= ChannelViewModel_PropertyChanged;

            if (_currentFixture.Channels.TryGetValue(channelVm.Name, out byte? value))
            {
                channelVm.Value = value ?? 0;
                Debug.WriteLine($"[DEBUG] Updated ChannelViewModel {channelVm.Name} to {value ?? 0}");
            }

            // Resubscribe
            channelVm.PropertyChanged += ChannelViewModel_PropertyChanged;
        }
    }

    // Deze methode blijft verantwoordelijk voor de LIVE update naar de hardware
    private async void ChannelViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChannelViewModel.Value) && sender is ChannelViewModel channelVm)
        {
            Debug.WriteLine($"[DEBUG] Channel value changed: {channelVm.Name} = {channelVm.Value}");

            // Update the fixture model (tijdelijk) en herbereken verhoudingen
            if (_currentFixture != null)
            {
                var channel = _currentFixture.Channels.FirstOrDefault(c => c.Name == channelVm.Name);
                if (channel != null)
                {
                    channel.Parameter = channelVm.Value;
                }

                Debug.WriteLine($"[DEBUG] Updated fixture model: {_currentFixture.InstanceId}.{channelVm.Name} = {channelVm.Value}");

                // Send to hardware connection (LIVE)
                var result = await _hardwareConnection.SetChannelValueAsync(
                    _currentFixture.InstanceId,
                    channelVm.Name,
                    channelVm.Value);

                Debug.WriteLine($"[DEBUG] SetChannelValueAsync returned: {result}");
            }
        }
    }
}