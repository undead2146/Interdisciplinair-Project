using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using InterdisciplinairProject.Core.Models;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public class ManufacturerGroup : INotifyPropertyChanged
    {
        public string Manufacturer { get; set; } = "";
        public ObservableCollection<Fixture> Fixtures { get; set; } = new();
        public ObservableCollection<Fixture> FilteredFixtures { get; set; } = new();

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

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
}
