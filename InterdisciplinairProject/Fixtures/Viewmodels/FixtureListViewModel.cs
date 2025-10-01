using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public class FixtureListViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Fixture> Fixtures { get; } = new();

        private Fixture _selectedFixture;
        public Fixture SelectedFixture
        {
            get => _selectedFixture;
            set { _selectedFixture = value; OnPropertyChanged(); }
        }

        public ICommand CreateFixtureCommand { get; }
        public ICommand EditChannelsCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
