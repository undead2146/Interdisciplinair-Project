using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace InterdisciplinairProject.ViewModels
{
/// <summary>
/// Main ViewModel for the InterdisciplinairProject application.
/// <remarks>
/// This ViewModel manages the state and commands for the main window, serving as the entry point for MVVM pattern.
/// It inherits from <see cref="ObservableObject" /> to enable property change notifications.
/// Properties and commands here can bind to UI elements in <see cref="MainWindow" />.
/// Future extensions will include navigation to feature ViewModels (e.g., FixtureViewModel from Features).
/// </remarks>
/// <seealso cref="ObservableObject" />
/// <seealso cref="MainWindow" />
/// </summary>
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "InterdisciplinairProject - DMX Lighting Control";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
        [ObservableProperty]
        private object? currentView;

        public RelayCommand OpenFixturesCommand { get; }
        public RelayCommand OpenFixtureSettingsCommand { get; }
        public RelayCommand OpenSceneCommand { get; }

        public MainViewModel()
        {
            Debug.WriteLine("[DEBUG] MainViewModel ctor");
            OpenFixturesCommand = new RelayCommand(OpenFixtures);
        // Initialize ViewModel, e.g., load services from DI if injected
            OpenFixtureSettingsCommand = new RelayCommand(OpenFixtureSettings);
            OpenSceneCommand = new RelayCommand(OpenScene);

            // Default landing view
            CurrentView = new TextBlock
            {
                Text = "Welcome � choose a module above",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 18
            };
        }

        private void OpenFixtures()
        {
            // IMPORTANT: MainWindowFixtures must be a UserControl now
            CurrentView = new InterdisciplinairProject.Fixtures.Views.MainWindowFixtures();
        }

        private void OpenFixtureSettings()
        {
            // Must be a UserControl now (we�ll convert the view below)
            CurrentView = new InterdisciplinairProject.Views.FixtureSettingsView();
        }

        private void OpenScene()
        {
            // Placeholder � you can replace with your real Scene view later
            CurrentView = new InterdisciplinairProject.Views.SceneView();
        }
    }
}