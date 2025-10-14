using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Fixtures.Views; // Vereist om Channel te kunnen gebruiken

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    // OPMERKING: U zou de Channel klasse idealiter ook verplaatsen naar Fixtures/Models/Channel.cs
    // Maar voor nu gebruiken we de versie die in Views/MainWindow.cs is gedefinieerd.

    public partial class ChannelViewModel : ObservableObject
    {
        // De eigenschap die de UI vertelt om te schakelen tussen TextBlock en TextBox
        [ObservableProperty]
        private bool isEditing;

        // De eigenschap die bepaalt of het kanaal is uitgeklapt (voor de dropdown)
        [ObservableProperty]
        private bool isExpanded;

        private Channel _model;

        // Eigenschap voor de naam, bindt direct aan het onderliggende model
        public string Name
        {
            get => _model.Name;
            set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
        }

        // Eigenschap voor het Type
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
