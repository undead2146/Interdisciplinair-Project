using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows.Input;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class MainWindowFixturesViewModel : ObservableObject
    {
        public ICommand CreateFixtureCommand { get; }
        public ICommand DeleteCommand { get; }

        public event EventHandler? DeleteRequested;

        [ObservableProperty]
        private object currentViewModel;

        private readonly FixtureListViewModel fixtureListVm;

        public MainWindowFixturesViewModel()
        {
            CreateFixtureCommand = new RelayCommand(CreateFixture);
            DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this, EventArgs.Empty));

            fixtureListVm = new FixtureListViewModel();
            fixtureListVm.FixtureSelected += OnFixtureSelected;

            CurrentViewModel = fixtureListVm;
        }

        private void OnFixtureSelected(object? sender, string json)
        {
            var detailVm = new FixtureContentViewModel(json);
            detailVm.BackRequested += (_, __) => CurrentViewModel = fixtureListVm;
            detailVm.EditRequested += (_, __) => { /* open edit window */ };
            detailVm.DeleteRequested += (_, __) => OnFixtureDelete(detailVm.Name);

            CurrentViewModel = detailVm;
        }

        private void CreateFixture()
        {
            var createVm = new FixtureCreateViewModel();

            // Terugkoppeling wanneer gebruiker op "Cancel" of "Save" drukt
            createVm.BackRequested += (_, __) =>
            {
                fixtureListVm.ReloadFixturesFromFiles();
                CurrentViewModel = fixtureListVm;
            };

            CurrentViewModel = createVm;
        }

        private void OnFixtureDelete(string fixtureName)
        {
            string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            string filePath = Path.Combine(dataFolder, fixtureName + ".json");

            if (!File.Exists(filePath))
            {
                System.Windows.MessageBox.Show("Bestand niet gevonden: " + filePath);
                return;
            }

            var confirm = System.Windows.MessageBox.Show(
                $"Are you sure that you want to delete '{fixtureName}'?",
                "Confirm deletion",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirm == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(filePath);
                    System.Windows.MessageBox.Show($"Fixture '{fixtureName}' is deleted.");

                    // terug naar lijst & opnbieuw laden
                    CurrentViewModel = fixtureListVm;
                    fixtureListVm.ReloadFixturesFromFiles();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Error with deletion: " + ex.Message);
                }
            }
        }
    }
}
