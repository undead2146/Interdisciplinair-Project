using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Fixtures.ViewModels;
using InterdisciplinairProject.Fixtures.Views;
using InterdisciplinairProject.ViewModels.Scene;
using Microsoft.Win32;
using Scene = InterdisciplinairProject.Core.Models.Scene;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the Fixture Registry List View.
/// Displays all fixtures available in the registry that can be added to scenes.
/// </summary>
public partial class FixtureRegistryListViewModel : ObservableObject
{
    private readonly IFixtureRegistry _fixtureRegistry;

    [ObservableProperty]
    private ObservableCollection<Fixture> _fixtures = new();

    [ObservableProperty]
    private Fixture? _selectedFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRegistryListViewModel"/> class.
    /// </summary>
    /// <param name="fixtureRegistry">The fixture registry service.</param>
    public FixtureRegistryListViewModel(IFixtureRegistry fixtureRegistry)
    {
        _fixtureRegistry = fixtureRegistry;
        _ = LoadFixturesAsync();
    }

    /// <summary>
    /// Event raised when a fixture is selected (double-clicked).
    /// The Action parameter will handle the selected fixture.
    /// </summary>
    public event EventHandler<Fixture>? FixtureSelected;

    /// <summary>
    /// Event raised when the user cancels the fixture selection.
    /// </summary>
    public event Action? Cancel;

    /// <summary>
    /// Loads all fixtures from the registry.
    /// </summary>
    private async Task LoadFixturesAsync()
    {
        try
        {
            Debug.WriteLine("[DEBUG] Loading fixtures from registry...");

            var fixtures = await _fixtureRegistry.GetAllFixturesAsync();

            Fixtures.Clear();
            foreach (var fixture in fixtures)
            {
                Fixtures.Add(fixture);
            }

            Debug.WriteLine($"[DEBUG] Loaded {Fixtures.Count} fixtures from registry");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error loading fixtures: {ex.Message}");
            MessageBox.Show(
                $"Fout bij laden van fixtures: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public async Task<int> ImportFixturesFromFileAsync(string filePath)
    {
        try
        {
            Debug.WriteLine($"[DEBUG] Importing fixtures from: {filePath}");
            var imported = await _fixtureRegistry.ImportFixturesAsync(filePath);

            // Refresh the list
            await LoadFixturesAsync();

            return imported;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error importing fixtures: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Called when a fixture is selected (double-clicked) in the list.
    /// Raises the FixtureSelected event.
    /// </summary>
    public void OnFixtureDoubleClicked(Fixture fixture)
    {
        if (fixture != null)
        {
            Debug.WriteLine($"[DEBUG] Fixture double-clicked: {fixture.Name}");
            FixtureSelected?.Invoke(this, fixture);
        }
    }

    /// <summary>
    /// Refreshes the fixture list from the registry.
    /// </summary>
    [RelayCommand]
    private async Task RefreshFixtures()
    {
        await _fixtureRegistry.RefreshRegistryAsync();
        await LoadFixturesAsync();
    }

    /// <summary>
    /// Cancels the fixture selection and returns to the scene list.
    /// </summary>
    [RelayCommand]
    private void CancelSelection()
    {
        Cancel?.Invoke();
    }

    /// <summary>
    /// Deletes a fixture from the registry.
    /// </summary>
    /// <param name="fixture">The fixture to delete.</param>
    [RelayCommand]
    private async Task DeleteFixture(Fixture fixture)
    {
        if (fixture == null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Weet je zeker dat je de fixture '{fixture.Name}' wilt verwijderen uit de registry?",
            "Bevestig Verwijdering",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _fixtureRegistry.RemoveFixtureAsync(fixture.InstanceId);
            await LoadFixturesAsync();
        }
    }

    /// <summary>
    /// Imports a fixture to the registry by selecting from available fixtures.
    /// </summary>
    [RelayCommand]
    private void ImportFixtureToRegistry()
    {
        try
        {
            var fixtureListViewModel = new FixtureListViewModel();
            fixtureListViewModel.FixtureSelected += OnFixtureSelectedForRegistryImport;

            var window = new Window
            {
                Title = "Select Fixture to Import",
                Content = new FixtureListView { DataContext = fixtureListViewModel },
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            window.ShowDialog();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error opening fixture list view: {ex.Message}");
            MessageBox.Show($"Error opening fixture list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Event handler for when a fixture is selected for registry import.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="json">The JSON string of the selected fixture.</param>
    private void OnFixtureSelectedForRegistryImport(object? sender, string json)
    {
        try
        {
            var fixture = JsonSerializer.Deserialize<Fixture>(json);
            if (fixture == null)
            {
                MessageBox.Show("Failed to deserialize fixture.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var viewModel = new FixtureRegistryImportViewModel(_fixtureRegistry, fixture);
            var dialog = new Views.Scene.FixtureRegistryDialog(viewModel);

            if (dialog.ShowDialog() == true)
            {
                // Refresh the list
                _ = LoadFixturesAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Error processing selected fixture: {ex.Message}");
            MessageBox.Show($"Error processing fixture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}