using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Fixtures.Views;

namespace InterdisciplinairProject.Fixtures.ViewModels
{


    public partial class ChannelViewModel : ObservableObject
    {
        // De eigenschap die de UI vertelt om te schakelen tussen TextBlock en TextBox
        [ObservableProperty]
        private bool isEditing;

        // De eigenschap die bepaalt of het kanaal is uitgeklapt (voor de dropdown)
        [ObservableProperty]
        private bool isExpanded;

        private Channel _model;
        public string Name
        {
            get => _model.Name;
            set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
        }
        public string Type => _model.Type;

        // Lijst van beschikbare kanaaltypes voor de dropdown
        [ObservableProperty]
        private ObservableCollection<string> availableTypes = new()
        {
            "Lamp",
            "Ster",
            "Klok",
            "Ventilator",
            "Rood",
            "Groen",
            "Blauw",
            "Wit"
        };

        // Geselecteerd type in de dropdown
        [ObservableProperty]
        private string selectedType;

        public ChannelViewModel(Channel model)
        {
            _model = model;
            selectedType = _model.Type; // initializeer de selectie met het huidige modeltype
        }
    }
}
