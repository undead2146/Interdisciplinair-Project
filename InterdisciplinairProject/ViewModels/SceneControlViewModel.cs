using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.ViewModels;
using Show.Model;
using System.ComponentModel;

public partial class SceneControlViewModel : ObservableObject
{
    private readonly Scene? _sceneModel;
    private readonly ShowbuilderViewModel? _parentShowVm;

    public SceneControlViewModel(Scene? scene = null, ShowbuilderViewModel? parentShowVm = null)
    {
        _sceneModel = scene;
        _parentShowVm = parentShowVm;

        if (_sceneModel != null)
        {
            _dimmer = _sceneModel.Dimmer;
            _isActive = _dimmer > 0;

            _sceneModel.PropertyChanged += SceneModel_PropertyChanged;
        }
    }

    private void SceneModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Scene.Dimmer) && _sceneModel != null)
        {
            if (Dimmer != _sceneModel.Dimmer)
                Dimmer = _sceneModel.Dimmer;
        }
    }

    public Scene? SceneModel => _sceneModel;
    public string? Id => _sceneModel?.Id;

    [ObservableProperty]
    private double _dimmer;

    partial void OnDimmerChanged(double value)
    {
        IsActive = value > 0.0;
        if (_sceneModel != null)
            _sceneModel.Dimmer = (int)Math.Round(value);

        if (_parentShowVm != null && _sceneModel != null)
        {
            _parentShowVm.UpdateSceneDimmer(_sceneModel, (int)Math.Round(value));

            // ensure the parent view model selects this scene so fixture details are shown
            if (_parentShowVm.SelectedScene != _sceneModel)
                _parentShowVm.SelectedScene = _sceneModel;
        }
    }

    [ObservableProperty]
    private bool _isActive;

    partial void OnIsActiveChanged(bool value)
    {
        if (!value)
        {
            Dimmer = 0;
        }
    }


    [RelayCommand]
    private void RequestToggle()
    {
        if (IsActive)
        {
            Dimmer = 0;
            IsActive = false;
        }
        else
        {
        }
    }
}