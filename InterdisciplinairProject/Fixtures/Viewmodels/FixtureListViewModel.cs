using InterdisciplinairProject.Fixtures.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Channels;
using InterdisciplinairProject.Fixtures.Views;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public class FixtureListViewModel
    {
        public ObservableCollection<Fixture> Fixtures { get; } = new();
        public ICommand CreateFixtureCommand { get; }

        public FixtureListViewModel()
        {
            CreateFixtureCommand = new RelayCommand(CreateFixture);
        }

        private void CreateFixture()
        {
            var window = new ChannelListView
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            window.ShowDialog();
        }
    }
}
