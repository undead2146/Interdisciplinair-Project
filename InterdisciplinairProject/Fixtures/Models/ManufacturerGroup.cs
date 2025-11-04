using System.Collections.ObjectModel;
using InterdisciplinairProject.Fixtures.Models;

public class ManufacturerGroup
{
    public string Manufacturer { get; set; }
    public ObservableCollection<Fixture> Fixtures { get; set; } = new();
    public ObservableCollection<Fixture> FilteredFixtures { get; set; } = new();

    public void RefreshFilteredFixtures(string searchText)
    {
        FilteredFixtures.Clear();

        IEnumerable<Fixture> filtered;
        if (string.IsNullOrWhiteSpace(searchText))
            filtered = Fixtures;
        else
            filtered = Fixtures
                .Where(f => f.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        foreach (var f in filtered)
            FilteredFixtures.Add(f);
    }
}

