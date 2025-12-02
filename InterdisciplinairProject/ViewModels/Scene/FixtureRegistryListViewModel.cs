using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Scene = InterdisciplinairProject.Core.Models.Scene;

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
}