using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var window = new ChannelListView
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
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
