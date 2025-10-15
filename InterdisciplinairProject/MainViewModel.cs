using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Repositories;
using InterdisciplinairProject.Features.Scene;
using InterdisciplinairProject.ViewModels;

namespace InterdisciplinairProject;

/// <summary>
/// Main ViewModel for the InterdisciplinairProject application.
/// </summary>
/// <remarks>
/// This ViewModel manages the state and commands for the main window, serving as the entry point for MVVM pattern.
/// It inherits from <see cref="ObservableObject" /> to enable property change notifications.
/// Properties and commands here can bind to UI elements in <see cref="MainWindow" />.
/// </remarks>
public partial class MainViewModel : ObservableObject
{
    private readonly IHardwareConnection _hardwareConnection;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly ISceneRepository _sceneRepository;

    [ObservableProperty]
    private string _title = "DMX Scene Builder";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
        : this(
            new MockHardwareConnection(),
            new FixtureRepository("../InterdisciplinairProject.Features/Scene/data/fixtures.json"),
            new SceneRepository("scenes.json"))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="hardwareConnection">The hardware connection service.</param>
    /// <param name="fixtureRepository">The fixture repository.</param>
    /// <param name="sceneRepository">The scene repository.</param>
    public MainViewModel(
        IHardwareConnection hardwareConnection,
        IFixtureRepository fixtureRepository,
        ISceneRepository sceneRepository)
    {
        _hardwareConnection = hardwareConnection;
        _fixtureRepository = fixtureRepository;
        _sceneRepository = sceneRepository;

        SceneListViewModel = new SceneListViewModel(_sceneRepository);
        SceneEditorViewModel = new SceneEditorViewModel(_sceneRepository, _fixtureRepository, _hardwareConnection);

        SceneListViewModel.PropertyChanged += OnSceneListPropertyChanged;
    }

    /// <summary>
    /// Gets the scene list view model.
    /// </summary>
    public SceneListViewModel SceneListViewModel { get; private set; }

    /// <summary>
    /// Gets the scene editor view model.
    /// </summary>
    public SceneEditorViewModel SceneEditorViewModel { get; private set; }

    private void OnSceneListPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SceneListViewModel.SelectedScene) && SceneListViewModel.SelectedScene != null)
        {
            SceneEditorViewModel.LoadScene(SceneListViewModel.SelectedScene);
        }
    }
}
