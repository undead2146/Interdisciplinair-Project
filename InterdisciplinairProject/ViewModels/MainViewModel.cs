using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InterdiscplinairProject.ViewModels;

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

    [ObservableProperty]
    private string? selectedScenePath;
    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
    {
        // Initialize ViewModel, e.g., load services from DI if injected
    }
    [RelayCommand]
    private void ImportScenes()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Scene",
            Filter = "JSON files (*.json)|*.json",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            selectedScenePath = openFileDialog.FileName;

            // TODO: Implement scene import logic
            // Example: await _sceneService.ImportSceneAsync(SelectedScenePath);

            System.Diagnostics.Debug.WriteLine($"Selected scene file: {selectedScenePath}");
        }
    }
}
