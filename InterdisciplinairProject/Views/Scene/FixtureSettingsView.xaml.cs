using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using InterdisciplinairProject.ViewModels;

namespace InterdisciplinairProject.Views
{
    /// <summary>
    /// Interaction logic for FixtureSettingsView.xaml.
    /// </summary>
    public partial class FixtureSettingsView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixtureSettingsView"/> class.
        /// </summary>
        public FixtureSettingsView()
        {
            Debug.WriteLine("[DEBUG] FixtureSettingsView constructor called");
            InitializeComponent();
            Debug.WriteLine("[DEBUG] FixtureSettingsView initialization complete");
        }

        // Hulpfunctie om de ViewModel op te halen
        private FixtureSettingsViewModel? GetViewModel()
        {
            return DataContext as FixtureSettingsViewModel;
        }

        // NIEUW: Hulpfunctie om de logica voor het opslaan naar de scene te simuleren
        private void SaveFixtureValuesToScene(Dictionary<string, byte?> channelValues)
        {
            // TODO: ECHTE OPSLAG IMPLEMENTEREN
            // De waarden moeten worden opgeslagen in uw scene-bestand (bijv. in de map:
            // C:\Users\Gebruiker\AppData\Local\InterdisciplinairProject)

            Debug.WriteLine("[SAVE] Simulating saving the following values to scene file:");
            foreach (var kvp in channelValues)
            {
                Debug.WriteLine($"[SAVE]   {kvp.Key}: {kvp.Value}");
            }
        }

        // Called when the "Save" button in the XAML is clicked.
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[DEBUG] Save button clicked - Staring save process.");

            var vm = GetViewModel();
            if (vm != null)
            {
                // 1. Haal de waarden op van de huidige slider-staat
                var valuesToSave = vm.GetCurrentChannelValues();

                // 2. SIMULEER: Voer de daadwerkelijke bestandsopslag uit
                SaveFixtureValuesToScene(valuesToSave);

                // 3. Bevestig de waarden als de nieuwe 'initial state' in de ViewModel voor toekomstige 'Cancel'-acties
                vm.ConfirmSave();

                // Optioneel: Navigeer weg of geef een melding
            }
        }

        // Called when the "Cancel" button in the XAML is clicked.
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[DEBUG] Cancel button clicked - Restoring initial state.");

            var vm = GetViewModel();
            if (vm != null)
            {
                // Roep de Cancel-methode aan.
                // Dit herstelt de ViewModel-waarden naar de laatst opgeslagen staat en stuurt deze LIVE naar de hardware.
                vm.CancelChanges();

                // Optioneel: Navigeer weg of geef een melding
            }
        }
    }
}