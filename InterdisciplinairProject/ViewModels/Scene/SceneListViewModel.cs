using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;

using Scene = InterdisciplinairProject.Core.Models.Scene;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the scene list.
/// </summary>
public partial class SceneListViewModel : ObservableObject
{
    private readonly ISceneRepository _sceneRepository;

    [ObservableProperty]
    private ObservableCollection<InterdisciplinairProject.Core.Models.Scene> _scenes = new();

    [ObservableProperty]
    private InterdisciplinairProject.Core.Models.Scene? _selectedScene;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneListViewModel"/> class.
    /// </summary>
    /// <param name="sceneRepository">The scene repository.</param>
    public SceneListViewModel(ISceneRepository sceneRepository)
    {
        _sceneRepository = sceneRepository;
        LoadScenes();
    }

    /// <summary>
    /// Loads the scenes from the repository.
    /// </summary>
    private async void LoadScenes()
    {
        var scenes = await _sceneRepository.GetAllScenesAsync();
        Scenes.Clear();
        foreach (var scene in scenes)
        {
            Scenes.Add(scene);
        }
    }

    /// <summary>
    /// Creates a new scene.
    /// </summary>
    [RelayCommand]
    private async Task CreateNewScene()
    {
        var newScene = new InterdisciplinairProject.Core.Models.Scene
        {
            Name = $"Nieuwe Scène {DateTime.Now:yyyy-MM-dd HH:mm}",
        };
        await _sceneRepository.SaveSceneAsync(newScene);
        Scenes.Add(newScene);
        SelectedScene = newScene;
    }

    /// <summary>
    /// Deletes the selected scene.
    /// </summary>
    [RelayCommand]
    private async Task DeleteSelectedScene()
    {
        if (SelectedScene != null && SelectedScene.Id != null)
        {
            await _sceneRepository.DeleteSceneAsync(SelectedScene.Id);
            Scenes.Remove(SelectedScene);
            SelectedScene = null;
        }
    }
}
