using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for fixture settings, showing channels with sliders.
/// </summary>
public partial class FixtureSettingsViewModel : ObservableObject
{
    private readonly IHardwareConnection _hardwareConnection;

    [ObservableProperty]
    private SceneFixture _sceneFixture = new();

    [ObservableProperty]
    private ObservableCollection<ChannelSettingViewModel> _channelSettings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureSettingsViewModel"/> class.
    /// </summary>
    /// <param name="hardwareConnection">The hardware connection.</param>
    public FixtureSettingsViewModel(IHardwareConnection hardwareConnection)
    {
        _hardwareConnection = hardwareConnection;
    }

    /// <summary>
    /// Loads the fixture settings.
    /// </summary>
    /// <param name="sceneFixture">The scene fixture.</param>
    public void LoadFixture(SceneFixture sceneFixture)
    {
        SceneFixture = sceneFixture;
        ChannelSettings.Clear();
        foreach (var channel in sceneFixture.Fixture.Channels)
        {
            var setting = new ChannelSettingViewModel(_hardwareConnection, sceneFixture, channel);
            ChannelSettings.Add(setting);
        }
    }
}
