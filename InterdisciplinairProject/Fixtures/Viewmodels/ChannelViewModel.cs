using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InterdisciplinairProject.Fixtures.Models;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models;
using System.Linq;

namespace InterdisciplinairProject.Fixtures.ViewModels
{
    public partial class ChannelViewModel : ObservableObject
    {

        // --- INTERACTIE EIGENSCHAPPEN (Gebruikt door FixtureCreateView.xaml.cs) ---
        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool isExpanded;

        // --- MODEL & WRAPPERS ---
        private Channel _model;

        // Wrapper voor Model.Name
        public string Name
        {
            get => _model.Name;
            set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
        }

        // Wrapper voor Model.Value (De Parameter is de string die wordt opgeslagen in JSON)
        public string? Parameter
        {
            get => _model.Value;
            set
            {
                if (_model.Value != value)
                {
                    _model.Value = value;
                    OnPropertyChanged();
                }
            }
        }

        // --- TYPE SELECTIE & WAARDE EIGENSCHAPPEN ---

        [ObservableProperty]
        private ObservableCollection<ChannelType> availableTypes = new()
        {
            ChannelType.Dimmer, ChannelType.Red, ChannelType.Green, ChannelType.Blue, ChannelType.White,
            ChannelType.Amber, ChannelType.Strobe, ChannelType.Pan, ChannelType.Tilt,
            ChannelType.ColorTemperature, ChannelType.Gobo, ChannelType.Color, ChannelType.Speed,
            ChannelType.Pattern, ChannelType.Power, ChannelType.Rate, ChannelType.Brightness
        };

        [ObservableProperty]
        private ChannelType selectedType; // Bindt aan de ComboBox

        [ObservableProperty]
        private int level = 0; // Bindt aan de Slider (0-255)

        // --- CONSTRUCTOR ---
        public ChannelViewModel(Channel model)
        {
            _model = model;
            selectedType = ChannelTypeHelper.GetChannelTypeFromName(_model.Type);

            // Initialisatie van Level op basis van modelwaarde
            if (int.TryParse(_model.Value ?? "0", out int currentLevel))
            {
                Level = currentLevel;
            }
        }

        // --- MVVM SYNCHRONISATIE METHODEN ---

        // Wordt automatisch aangeroepen wanneer SelectedType wijzigt
        partial void OnSelectedTypeChanged(ChannelType value)
        {
            _model.Type = ChannelTypeHelper.GetDisplayName(value);
            Level = 0; // Reset level bij typeverandering

            // Forceer Level om de Parameter/Value te schrijven als het een 'level' type is
            if (new[] { ChannelType.Red, ChannelType.Green, ChannelType.Blue, ChannelType.White }.Contains(value))
            {
                OnLevelChanged(Level);
            }
        }

        // Wordt automatisch aangeroepen wanneer Level wijzigt
        partial void OnLevelChanged(int value)
        {
            // Cruciale fix: zet de int Level om naar string voor opslag in _model.Value
            _model.Value = value.ToString();

            // Notificatie om UI elementen die aan Parameter zijn gebonden, bij te werken (bijv. TextBox)
            OnPropertyChanged(nameof(Parameter));
        }
    }
}
