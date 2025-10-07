using InterdisciplinairProject.Fixtures.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Views;
using System.IO;
using System.Threading;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public class FixtureListViewModel
    {
        public ObservableCollection<Fixture> Fixtures { get; } = new();

        public ICommand CreateFixtureCommand { get; }

        private readonly string _dataFolder;
        private FileSystemWatcher _watcher;

        public FixtureListViewModel()
        {
            CreateFixtureCommand = new RelayCommand(CreateFixture);

            // Path to the "data" folder relative to bin\Debug\net8.0-windows
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _dataFolder = Path.Combine(baseDir, "data");

            LoadFixturesFromFiles();
            StartWatchingDataFolder();
        }

        private void CreateFixture()
        {
            var window = new ChannelListView
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        private void LoadFixturesFromFiles()
        {
            if (!Directory.Exists(_dataFolder))
            {
                System.Windows.MessageBox.Show("Data folder not found: " + _dataFolder);
                return;
            }

            var jsonFiles = Directory.GetFiles(_dataFolder, "*.json");

            foreach (var file in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (!FixturesExists(fileName))
                    Fixtures.Add(new Fixture(fileName));
            }
        }

        private bool FixturesExists(string name)
        {
            foreach (var f in Fixtures)
            {
                if (f.Name == name)
                    return true;
            }
            return false;
        }

        private void StartWatchingDataFolder()
        {
            if (!Directory.Exists(_dataFolder))
                return;

            _watcher = new FileSystemWatcher(_dataFolder, "*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            _watcher.Created += OnFileCreated;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            // Wait a bit to ensure the file is fully written
            Thread.Sleep(100);

            string fileName = Path.GetFileNameWithoutExtension(e.Name);

            // Update collection on the UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (!FixturesExists(fileName))
                    Fixtures.Add(new Fixture(fileName));
            });
        }
    }
}
