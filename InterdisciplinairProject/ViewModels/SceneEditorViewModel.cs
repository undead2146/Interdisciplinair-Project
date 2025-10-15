using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for editing a scene.
/// </summary>
public partial class SceneEditorViewModel : ObservableObject
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IHardwareConnection _hardwareConnection;

    [ObservableProperty]
    private Scene _scene = new();

    [ObservableProperty]
    private ObservableCollection<SceneFixture> _sceneFixtures = new();

    [ObservableProperty]
    private FixtureSettingsViewModel _fixtureSettingsViewModel;

    [ObservableProperty]
    private SceneFixture? _selectedFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneEditorViewModel"/> class.
    /// </summary>
    /// <param name="sceneRepository">The scene repository.</param>
    /// <param name="fixtureRepository">The fixture repository.</param>
    /// <param name="hardwareConnection">The hardware connection.</param>
    public SceneEditorViewModel(ISceneRepository sceneRepository, IFixtureRepository fixtureRepository, IHardwareConnection hardwareConnection)
    {
        _sceneRepository = sceneRepository;
        _fixtureRepository = fixtureRepository;
        _hardwareConnection = hardwareConnection;
        _fixtureSettingsViewModel = new FixtureSettingsViewModel(hardwareConnection);
    }

    /// <summary>
    /// Loads a scene for editing.
    /// </summary>
    /// <param name="scene">The scene to load.</param>
    public void LoadScene(Scene scene)
    {
        Scene = scene;
        SceneFixtures.Clear();
        foreach (var fixture in scene.Fixtures)
        {
            SceneFixtures.Add(fixture);
        }
    }

    /// <summary>
    /// Adds a fixture to the scene.
    /// </summary>
    [RelayCommand]
    private async Task AddFixture()
    {
        var fixtures = await _fixtureRepository.GetAllFixturesAsync();

        // For simplicity, add the first fixture. In real app, show dialog.
        if (fixtures.Any())
        {
            var fixture = fixtures.First();
            var sceneFixture = new SceneFixture
            {
                Fixture = fixture,
                Universe = 1,
                StartChannel = GetNextAvailableChannel(),
            };

            // Initialize channel values to defaults
            foreach (var channel in fixture.Channels)
            {
                sceneFixture.ChannelValues[channel.Name] = channel.Default;
            }

            Scene.Fixtures.Add(sceneFixture);
            SceneFixtures.Add(sceneFixture);
        }
    }

    /// <summary>
    /// Saves the scene.
    /// </summary>
    [RelayCommand]
    private async Task SaveScene()
    {
        await _sceneRepository.SaveSceneAsync(Scene);
    }

    partial void OnSelectedFixtureChanged(SceneFixture? value)
    {
        if (value != null)
        {
            FixtureSettingsViewModel.LoadFixture(value);
        }
    }

    private int GetNextAvailableChannel()
    {
        // Simple logic: find the highest end channel and add 1
        var maxChannel = 0;
        foreach (var sf in SceneFixtures)
        {
            var endChannel = sf.StartChannel + sf.Fixture.Channels.Count - 1;
            if (endChannel > maxChannel)
            {
                maxChannel = endChannel;
            }
        }

        return maxChannel + 1;
    }
}
