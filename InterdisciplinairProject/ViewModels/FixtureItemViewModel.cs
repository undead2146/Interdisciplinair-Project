using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for displaying a fixture item in the import fixtures list.
/// </summary>
public partial class FixtureItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureItemViewModel"/> class.
    /// </summary>
    /// <param name="fixture">The fixture model.</param>
    public FixtureItemViewModel(Fixture fixture)
    {
        Fixture = fixture;
        Channels = new ObservableCollection<FixtureChannelInfoViewModel>();

        // Build channel info list
        int channelNumber = 1;
        foreach (var channel in fixture.Channels)
        {
            var description = fixture.ChannelDescriptions.TryGetValue(channel.Name, out var desc)
                ? desc
                : string.Empty;

            Channels.Add(new FixtureChannelInfoViewModel(channelNumber, channel.Name, description));
            channelNumber++;
        }
    }

    /// <summary>
    /// Gets the fixture model.
    /// </summary>
    public Fixture Fixture { get; }

    /// <summary>
    /// Gets the fixture ID.
    /// </summary>
    public string FixtureId => Fixture.Id;

    /// <summary>
    /// Gets the fixture name.
    /// </summary>
    public string Name => Fixture.Name;

    /// <summary>
    /// Gets the manufacturer.
    /// </summary>
    public string Manufacturer => Fixture.Manufacturer;

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description => Fixture.Description;

    /// <summary>
    /// Gets the total number of channels.
    /// </summary>
    public int ChannelCount => Fixture.ChannelCount;

    /// <summary>
    /// Gets a value indicating whether this fixture is complex.
    /// </summary>
    public bool IsComplex => Fixture.IsComplex;

    /// <summary>
    /// Gets the complexity warning message.
    /// </summary>
    public string ComplexityWarning => IsComplex
        ? "⚠ Complex fixture, requires advanced knowledge"
        : string.Empty;

    /// <summary>
    /// Gets the channels collection.
    /// </summary>
    public ObservableCollection<FixtureChannelInfoViewModel> Channels { get; }

    /// <summary>
    /// Gets the formatted fixture info for display.
    /// </summary>
    public string FormattedInfo
    {
        get
        {
            var info = $"{Name}";
            if (!string.IsNullOrEmpty(Manufacturer))
            {
                info += $" by {Manufacturer}";
            }

            info += $" ({ChannelCount} channels)";
            return info;
        }
    }

    /// <summary>
    /// Gets the channel summary (first few channel types).
    /// </summary>
    public string ChannelSummary
    {
        get
        {
            var channelTypes = Channels
                .Take(4)
                .Select(c => c.DisplayName)
                .ToList();

            var summary = string.Join(", ", channelTypes);
            if (Channels.Count > 4)
            {
                summary += $", +{Channels.Count - 4} more";
            }

            return summary;
        }
    }
}
