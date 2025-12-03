using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models.Enums;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace InterdisciplinairProject.ViewModels.Scene;

/// <summary>
/// ViewModel for the Fixture Registry Dialog.
/// </summary>
public partial class FixtureRegistryImportViewModel : ObservableObject
{
    private readonly IFixtureRegistry _fixtureRegistry;
    private Fixture? _selectedFixture;
    private int _startAddress = 1;
    private string _fixtureName = string.Empty;
    private ObservableCollection<EffectRow> _effectRows = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRegistryImportViewModel"/> class.
    /// </summary>
    /// <param name="fixtureRegistry">The fixture registry.</param>
    /// <param name="selectedFixture">The selected fixture to configure.</param>
    public FixtureRegistryImportViewModel(IFixtureRegistry fixtureRegistry, Fixture? selectedFixture = null)
    {
        _fixtureRegistry = fixtureRegistry;
        SelectedFixture = selectedFixture;
        if (SelectedFixture != null)
        {
            FixtureName = SelectedFixture.Name ?? string.Empty;
            StartAddress = SelectedFixture.StartAddress;

            // Initialize effect rows if needed
        }
    }

    /// <summary>
    /// Gets the end address based on start address and channel count.
    /// </summary>
    public int EndAddress => SelectedFixture != null ? StartAddress + (SelectedFixture.Channels?.Count ?? 0) - 1 : 0;

    /// <summary>
    /// Gets the available effect types.
    /// </summary>
    public Array EffectTypes => Enum.GetValues(typeof(EffectType));

    /// <summary>
    /// Gets the available channels for the selected fixture.
    /// </summary>
    public ObservableCollection<Channel> AvailableChannels => SelectedFixture?.Channels ?? new ObservableCollection<Channel>();

    /// <summary>
    /// Gets or sets the selected fixture.
    /// </summary>
    public Fixture? SelectedFixture
    {
        get => _selectedFixture;
        set
        {
            if (_selectedFixture != value)
            {
                _selectedFixture = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndAddress));
                OnPropertyChanged(nameof(AvailableChannels));
            }
        }
    }

    /// <summary>
    /// Gets or sets the start address.
    /// </summary>
    public int StartAddress
    {
        get => _startAddress;
        set
        {
            if (_startAddress != value)
            {
                _startAddress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EndAddress));
            }
        }
    }

    /// <summary>
    /// Gets or sets the fixture name.
    /// </summary>
    public string FixtureName
    {
        get => _fixtureName;
        set
        {
            if (_fixtureName != value)
            {
                _fixtureName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the effect rows for the fixture.
    /// </summary>
    public ObservableCollection<EffectRow> EffectRows => _effectRows;

    /// <summary>
    /// Adds a new effect row.
    /// </summary>
    [RelayCommand]
    public void AddEffectRow()
    {
        if (SelectedFixture?.Channels != null)
        {
            // Pass the list of all channels to the new EffectRow instance
            EffectRows.Add(new EffectRow(SelectedFixture.Channels));
        }
        else
        {
            // Fallback for an empty or unselected fixture
            EffectRows.Add(new EffectRow(Enumerable.Empty<Channel>()));
        }
    }

    /// <summary>
    /// Saves the fixture to the registry.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task Save()
    {
        if (SelectedFixture == null)
        {
            MessageBox.Show("Geen fixture geselecteerd.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            // Update fixture properties
            SelectedFixture.StartAddress = StartAddress;
            SelectedFixture.Name = FixtureName;

            // Apply effects to channels
            foreach (var effectRow in EffectRows)
            {
                if (effectRow.SelectedChannel != null)
                {
                    if (byte.TryParse(effectRow.Min, out var min) &&
                        byte.TryParse(effectRow.Max, out var max) &&
                        int.TryParse(effectRow.Time, out var time))
                    {
                        effectRow.SelectedChannel.ChannelEffect.EffectType = effectRow.EffectType;
                        effectRow.SelectedChannel.ChannelEffect.Min = min;
                        effectRow.SelectedChannel.ChannelEffect.Max = max;
                        effectRow.SelectedChannel.ChannelEffect.Time = time;
                    }
                }
            }

            // Save to registry
            await _fixtureRegistry.AddFixtureAsync(SelectedFixture);

            Debug.WriteLine($"[DEBUG] Fixture '{SelectedFixture.Name}' saved to registry");
            MessageBox.Show("Fixture succesvol opgeslagen in registry!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error saving fixture to registry: {ex.Message}");
            MessageBox.Show($"Fout bij opslaan van fixture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Cancels the dialog.
    /// </summary>
    [RelayCommand]
    public void Cancel()
    {
        // Close dialog logic handled in code-behind
    }
}