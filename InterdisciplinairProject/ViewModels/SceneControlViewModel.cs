using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.ViewModels;
using Show.Model;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using InterdisciplinairProject.Views;

public partial class SceneControlViewModel : ObservableObject
{
    private readonly Scene? _sceneModel;
    private readonly ShowbuilderViewModel? _parentShowVm;

    // cancellation for play animation
    private CancellationTokenSource? _playCts;

    // suppress pushing changes back to parent when they originate from the model (e.g. fades)
    private bool _suppressParentUpdate;

    public SceneControlViewModel(Scene? scene = null, ShowbuilderViewModel? parentShowVm = null)
    {
        _sceneModel = scene;
        _parentShowVm = parentShowVm;

        if (_sceneModel != null)
        {
            _dimmer = _sceneModel.Dimmer;
            _fadeInMs = _sceneModel.FadeInMs;
            _fadeOutMs = _sceneModel.FadeOutMs;
            _isActive = _dimmer > 0;

            _sceneModel.PropertyChanged += SceneModel_PropertyChanged;
        }
    }

    private void SceneModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_sceneModel == null) return;

        if (e.PropertyName == nameof(Scene.Dimmer))
        {
            // Mark suppression so OnDimmerChanged doesn't call UpdateSceneDimmer
            _suppressParentUpdate = true;
            try
            {
                if (Dimmer != _sceneModel.Dimmer)
                    Dimmer = _sceneModel.Dimmer;
            }
            finally
            {
                _suppressParentUpdate = false;
            }
        }
        else if (e.PropertyName == nameof(Scene.FadeInMs))
        {
            if (FadeInMs != _sceneModel.FadeInMs)
                FadeInMs = _sceneModel.FadeInMs;
        }
        else if (e.PropertyName == nameof(Scene.FadeOutMs))
        {
            if (FadeOutMs != _sceneModel.FadeOutMs)
                FadeOutMs = _sceneModel.FadeOutMs;
        }
    }

    public Scene? SceneModel => _sceneModel;
    public string? Id => _sceneModel?.Id;

    [ObservableProperty]
    private double _dimmer;

    partial void OnDimmerChanged(double value)
    {
        IsActive = value > 0.0;

        // If the change came from the model (fade), do not call UpdateSceneDimmer.
        if (_suppressParentUpdate)
            return;

        // Persist when slider moved manually (UpdateSceneDimmer handles zeroing other scenes and fixture updates).
        if (_sceneModel != null && _parentShowVm != null)
        {
            _parentShowVm.UpdateSceneDimmer(_sceneModel, (int)Math.Round(value));
        }
        else if (_sceneModel != null)
        {
            _sceneModel.Dimmer = (int)Math.Round(value);
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

    [ObservableProperty]
    private int _fadeInMs;

    partial void OnFadeInMsChanged(int value)
    {
        if (_sceneModel != null)
            _sceneModel.FadeInMs = value;
    }

    [ObservableProperty]
    private int _fadeOutMs;

    partial void OnFadeOutMsChanged(int value)
    {
        if (_sceneModel != null)
            _sceneModel.FadeOutMs = value;
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

    // New command to open settings window. The window's DataContext will be this view model.
    [RelayCommand]
    private void OpenSettings()
    {
        var window = new SceneSettingsWindow
        {
            DataContext = this,
            Owner = Application.Current?.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        window.ShowDialog();
    }

    // Play command: always fade to 100% over the configured FadeInMs.
    [RelayCommand]
    private async Task PlayAsync()
    {
        if (_sceneModel == null)
            return;

        // ensure fixtures list is visible immediately
        if (_parentShowVm != null && _sceneModel != null)
        {
            _parentShowVm.SelectedScene = _sceneModel;
        }

        // If we have a parent show VM, ask it to orchestrate fade-out of others
        if (_parentShowVm != null)
        {
            try
            {
                await _parentShowVm.FadeToAndActivateAsync(_sceneModel, 100);
            }
            catch (OperationCanceledException) { }
            return;
        }

        // fallback: animate locally
        // cancel any running animation
        _playCts?.Cancel();
        _playCts?.Dispose();
        _playCts = new CancellationTokenSource();
        var ct = _playCts.Token;

        double target = 100.0;
        int duration = Math.Max(0, _sceneModel.FadeInMs);

        try
        {
            await AnimateToAsync(target, duration, ct);
        }
        catch (OperationCanceledException)
        {
            // canceled - no further action
        }
    }

    // animate Dimmer over duration (ms). Updates model and fixtures through ShowbuilderViewModel so:
    // - fixture channels change while animating
    // - other scenes are zeroed when this scene turns on
    private async Task AnimateToAsync(double target, int durationMs, CancellationToken ct)
    {
        if (_sceneModel == null)
            return;

        // immediate set if duration is 0 or negative
        if (durationMs <= 0)
        {
            if (_parentShowVm != null)
                _parentShowVm.UpdateSceneDimmer(_sceneModel, (int)Math.Round(target));
            else
                _sceneModel.Dimmer = (int)Math.Round(target);

            return;
        }

        const int intervalMs = 20;
        int steps = Math.Max(1, durationMs / intervalMs);
        double start = _sceneModel.Dimmer; // use model as authoritative start
        double delta = (target - start) / steps;

        for (int i = 1; i <= steps; i++)
        {
            ct.ThrowIfCancellationRequested();

            double next = start + delta * i;
            int nextInt = (int)Math.Round(Math.Max(0, Math.Min(100, next)));

            // Use parent VM method so fixtures are updated and other scenes are turned off.
            if (_parentShowVm != null)
            {
                _parentShowVm.UpdateSceneDimmer(_sceneModel, nextInt);
                // SceneModel_PropertyChanged will pick up the model change and update this VM's Dimmer property.
            }
            else
            {
                // fallback: update model (will trigger property change)
                _sceneModel.Dimmer = nextInt;
                Dimmer = nextInt;
            }

            await Task.Delay(intervalMs, ct);
        }

        // ensure exact target at end
        if (_parentShowVm != null)
            _parentShowVm.UpdateSceneDimmer(_sceneModel, (int)Math.Round(target));
        else
            _sceneModel.Dimmer = (int)Math.Round(target);
    }
}
