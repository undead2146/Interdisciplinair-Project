using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.ViewModels;
using InterdisciplinairProject.Core.Models;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace InterdisciplinairProject.ViewModels
{
    public partial class TimeLineViewModel : ObservableObject
    {
        private readonly ShowScene? _sceneModel;
        private readonly ShowbuilderViewModel? _parentShowVm;

        // cancellation for play animation
        private CancellationTokenSource? _playCts;

        // suppress pushing changes back to parent when they originate from the model (e.g. fades)
        private bool _suppressParentUpdate;

        public TimeLineViewModel(ShowScene? scene = null, ShowbuilderViewModel? parentShowVm = null)
        {
            _sceneModel = scene;
            _parentShowVm = parentShowVm;

            if (_sceneModel != null)
            {
                _sceneModel.PropertyChanged += SceneModel_PropertyChanged;
            }
        }

        private void SceneModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_sceneModel == null) return;

            if (e.PropertyName == nameof(ShowScene.Id) || e.PropertyName == nameof(ShowScene.Name))
            {
                OnPropertyChanged(nameof(Id));
                OnPropertyChanged(nameof(Name));
            }
        }

        public ShowScene? SceneModel => _sceneModel;
        public string? Id => _sceneModel?.Id;
        public string? Name => _sceneModel?.Name;

        // Play command: always fade to 100% over the configured FadeInMs.
        [RelayCommand]
        private async Task PlayAsync()
        {
            if (_sceneModel == null)
                return;

            // ensure fixtures list is visible immediately
            if (_parentShowVm != null && _sceneModel != null)
            {
                _parentShowVm.SelectedTimelineScene = _sceneModel;
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
        }

        // MoveScene command - delegates to parent ShowbuilderViewModel
        [RelayCommand]
        private void MoveScene(string direction)
        {
            if (_parentShowVm != null && _sceneModel != null)
            {
                _parentShowVm.MoveTimelineScene(_sceneModel, direction);
            }
        }

        // OpenSettings command - opens the scene settings window
        [RelayCommand]
        private void OpenSettings()
        {
            if (_sceneModel == null)
                return;

            var window = new InterdisciplinairProject.Views.SceneSettingsWindow
            {
                Owner = Application.Current?.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // Create a SceneControlViewModel for the settings window
            var settingsVm = new SceneControlViewModel(_sceneModel, _parentShowVm);
            window.DataContext = settingsVm;

            window.ShowDialog();
        }
    }
}
