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

        private Channel _model;

        // Eigenschap voor de naam, bindt direct aan het onderliggende model
        public string Name
        {
            get => _model.Name;
            set => SetProperty(_model.Name, value, _model, (m, v) => m.Name = v);
        }

        // Eigenschap voor het Type
        public string Type => _model.Type;

        public ChannelViewModel(Channel model)
        {
            _model = model;
        }
    }
}
