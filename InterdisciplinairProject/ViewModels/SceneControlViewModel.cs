using System;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Show.Model;

namespace InterdisciplinairProject.ViewModels
{
    public partial class SceneControlViewModel : ObservableObject
    {
        private readonly Scene? _sceneModel;

        public SceneControlViewModel(Scene? scene = null)
        {
            _sceneModel = scene;
            if (_sceneModel != null)
            {
                // initialize from model
                _dimmer = _sceneModel.Dimmer;
                _isActive = _dimmer > 0;
            }
        }

        [ObservableProperty]
        private double _dimmer;

        partial void OnDimmerChanged(double value)
        {
            // changing dimmer affects active state
            IsActive = value > 0.0;
            // persist back to model if available (round to int to match model)
            if (_sceneModel != null)
                _sceneModel.Dimmer = (int)Math.Round(value);
        }

        [ObservableProperty]
        private bool _isActive;

        /// <summary>
        /// Request to toggle the LED. Behavior: only allow green->red via toggle (which sets dimmer to 0).
        /// Red->green via toggle is not allowed — user must slide up to reactivate.
        /// </summary>
        public void RequestToggle()
        {
            if (IsActive)
            {
                // turn off
                Dimmer = 0;
                IsActive = false;
            }
            else
            {
                // if inactive, we don't toggle to active here — user must slide up
            }
        }
    }
}
