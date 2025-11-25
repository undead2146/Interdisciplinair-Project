using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Services;
using Microsoft.Win32;

namespace InterdisciplinairProject.ViewModels;

/// <summary>
/// ViewModel for the Import Fixtures dialog.
/// </summary>
public partial class ImportFixturesViewModel : ObservableObject
{
    /// <summary>
    /// Event raised when the dialog should be closed.
    /// </summary>
    public event EventHandler? CloseRequested;

    private readonly FixtureRepository _fixtureRepository;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedFilterType = "All";

    [ObservableProperty]
    private FixtureItemViewModel? _selectedFixture;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Klaar";

    [ObservableProperty]
    private string _currentSceneName = string.Empty;

    [ObservableProperty]
    private InterdisciplinairProject.Core.Models.Scene? _currentScene;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportFixturesViewModel"/> class.
    /// </summary>
    public ImportFixturesViewModel()
    {
        _fixtureRepository = new FixtureRepository();
        AllFixtures = new ObservableCollection<FixtureItemViewModel>();
        FilteredFixtures = new ObservableCollection<FixtureItemViewModel>();
        SelectedFixtures = new List<Fixture>();
        FilterTypes = new ObservableCollection<string> { "All", "LED", "Moving Head", "Wash", "Spot", "Laser", "Strobe" };

        _ = LoadFixturesAsync();

        // CurrentSceneName will be set when CurrentScene is assigned
    }

    /// <summary>
    /// Gets the collection of all fixtures.
    /// </summary>
    public ObservableCollection<FixtureItemViewModel> AllFixtures { get; }

    /// <summary>
    /// Gets the collection of filtered fixtures.
    /// </summary>
    public ObservableCollection<FixtureItemViewModel> FilteredFixtures { get; }

    /// <summary>
    /// Gets the list of selected fixtures.
    /// </summary>
    public List<Fixture> SelectedFixtures { get; }

    /// <summary>
    /// Gets the available filter types.
    /// </summary>
    public ObservableCollection<string> FilterTypes { get; }

    /// <summary>
    /// Loads all fixtures from the repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task LoadFixturesAsync()
    {
        IsLoading = true;
        StatusMessage = "Fixtures laden...";

        try
        {
            var fixtures = await _fixtureRepository.GetAllFixturesAsync();

            AllFixtures.Clear();
            foreach (var fixture in fixtures)
            {
                AllFixtures.Add(new FixtureItemViewModel(fixture));
            }

            ApplyFilters();

            // Select the first fixture by default so details are visible
            SelectedFixture = FilteredFixtures.FirstOrDefault();
            StatusMessage = $"{AllFixtures.Count} fixtures geladen";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] ImportFixturesViewModel: {ex.Message}");
            StatusMessage = "Fout bij laden fixtures";
            MessageBox.Show($"Fout bij laden fixtures: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Imports a fixture from a file.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "JSON Bestanden (*.json)|*.json|Alle Bestanden (*.*)|*.*",
            Title = "Fixture Definitie Importeren",
            Multiselect = false,
        };

        if (openFileDialog.ShowDialog() == true)
        {
            IsLoading = true;
            StatusMessage = "Fixture importeren...";

            try
            {
                var success = await _fixtureRepository.ImportFixtureFromFileAsync(openFileDialog.FileName);

                if (success)
                {
                    await LoadFixturesAsync();
                    StatusMessage = "Fixture succesvol geïmporteerd";
                    MessageBox.Show("Fixture succesvol geïmporteerd!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "Importeren fixture mislukt";
                    MessageBox.Show("Importeren fixture mislukt. Controleer het bestandsformaat.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] ImportFixturesViewModel: {ex.Message}");
                StatusMessage = "Fout bij importeren fixture";
                MessageBox.Show($"Fout bij importeren fixture: {ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    /// <summary>
    /// Adds the selected fixtures to the scene.
    /// </summary>
    [RelayCommand]
    private void AddSelectedFixtures()
    {
        if (CurrentScene == null)
        {
            MessageBox.Show("Geen scene geselecteerd.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SelectedFixtures.Clear();

        foreach (var fixtureItem in AllFixtures.Where(f => f.IsSelected))
        {
            SelectedFixtures.Add(fixtureItem.Fixture);
        }

        if (SelectedFixtures.Count == 0)
        {
            MessageBox.Show("Selecteer ten minste één fixture om toe te voegen.", "Geen Selectie", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Add selected fixtures to the current scene
        if (CurrentScene.Fixtures == null)
        {
            CurrentScene.Fixtures = new List<InterdisciplinairProject.Core.Models.Fixture>();
        }

        foreach (var fixture in SelectedFixtures)
        {
            CurrentScene.Fixtures.Add(fixture);
        }

        StatusMessage = $"{SelectedFixtures.Count} fixture(s) toegevoegd aan scene '{CurrentScene.Name ?? "Unnamed"}'";
        CurrentSceneName = CurrentScene.Name ?? "Unnamed Scene";
        Debug.WriteLine($"[DEBUG] ImportFixturesViewModel: Added {SelectedFixtures.Count} fixtures to scene '{CurrentScene.Name ?? "Unnamed"}'");

        // Close the dialog after successful addition
        MessageBox.Show($"{SelectedFixtures.Count} fixture(s) succesvol toegevoegd aan de scene!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Searches fixtures based on search text.
    /// </summary>
    [RelayCommand]
    private void Search()
    {
        ApplyFilters();
    }

    /// <summary>
    /// Applies filters to the fixture list.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    /// <summary>
    /// Applies filters when filter type changes.
    /// </summary>
    partial void OnSelectedFilterTypeChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        FilteredFixtures.Clear();

        var query = AllFixtures.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            query = query.Where(f =>
                f.Name.ToLowerInvariant().Contains(searchLower) ||
                f.Manufacturer.ToLowerInvariant().Contains(searchLower) ||
                f.FixtureId.ToLowerInvariant().Contains(searchLower) ||
                f.ChannelSummary.ToLowerInvariant().Contains(searchLower));
        }

        // Apply type filter
        if (!string.IsNullOrEmpty(SelectedFilterType) && SelectedFilterType != "All")
        {
            var filterLower = SelectedFilterType.ToLowerInvariant();
            query = query.Where(f =>
                f.Name.ToLowerInvariant().Contains(filterLower) ||
                f.Description.ToLowerInvariant().Contains(filterLower));
        }

        foreach (var fixture in query)
        {
            FilteredFixtures.Add(fixture);
        }

        StatusMessage = $"{FilteredFixtures.Count} van {AllFixtures.Count} fixtures weergegeven";
    }
}
