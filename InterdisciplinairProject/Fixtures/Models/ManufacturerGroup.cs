using System;
using System.Collections.ObjectModel;
using System.Linq;
using InterdisciplinairProject.Fixtures.Models;

public class ManufacturerGroup
{
    public string Manufacturer { get; set; } = "";
    public ObservableCollection<Fixture> Fixtures { get; set; } = new();
    public ObservableCollection<Fixture> FilteredFixtures { get; set; } = new();

    public void RefreshFilteredFixtures(string searchText, InterdisciplinairProject.Fixtures.ViewModels.FilterMode filterMode)
    {
        FilteredFixtures.Clear();

        IEnumerable<Fixture> filtered;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            filtered = Fixtures;
        }
        else
        {
            if (filterMode == InterdisciplinairProject.Fixtures.ViewModels.FilterMode.Fixture)
                filtered = Fixtures.Where(f => f.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            else
                filtered = Fixtures.Where(f => f.Manufacturer.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var f in filtered)
            FilteredFixtures.Add(f);
    }
}
