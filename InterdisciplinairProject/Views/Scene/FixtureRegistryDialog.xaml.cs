using InterdisciplinairProject.Fixtures.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace InterdisciplinairProject.Views.Scene
{
    /// <summary>
    /// Interaction logic for FixtureRegistryDialog.xaml
    /// </summary>
    public partial class FixtureRegistryDialog : Window
    {
        public Fixture SelectedFixture
        {
            get { return (Fixture)GetValue(SelectedFixtureProperty); }
            set { SetValue(SelectedFixtureProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedFixture.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedFixtureProperty =
            DependencyProperty.Register("SelectedFixture", typeof(Fixture), typeof(FixtureRegistryDialog), new PropertyMetadata(null));

        public FixtureRegistryDialog()
        {
            InitializeComponent();
            this.DataContext = this; // Set DataContext for direct binding in XAML
        }

        public FixtureRegistryDialog(Fixture fixture) : this()
        {
            SelectedFixture = fixture;

            if (SelectedFixture.Channels.Count == 0)
            {
                SelectedFixture.Channels.Add(new Channel { Name = "Intensiteit", Type = "Dimmer" });
                SelectedFixture.Channels.Add(new Channel { Name = "Rood (CH-1)", Type = "RGB" });
                SelectedFixture.Channels.Add(new Channel { Name = "Groen (CH-2)", Type = "RGB" });
                SelectedFixture.Channels.Add(new Channel { Name = "Blauw (CH-3)", Type = "RGB" });
            }
        }

        /// <summary>
        /// Constructor overload to accept Core.Models.Fixture
        /// </summary>
        public FixtureRegistryDialog(InterdisciplinairProject.Core.Models.Fixture coreFixture) : this()
        {
            // Adapt Core.Models.Fixture to Fixtures.Models.Fixture format
            var adaptedFixture = new Fixture
            {
                Name = coreFixture.Name ?? "Unknown",
                Manufacturer = coreFixture.Manufacturer ?? "Unknown"
            };

            // Convert channels from Dictionary to ObservableCollection
            if (coreFixture.Channels != null)
            {
                foreach (var channelKey in coreFixture.Channels.Keys)
                {
                    adaptedFixture.Channels.Add(new Channel { Name = channelKey, Type = "DMX" });
                }
            }
            else
            {
                // Add default channels if none exist
                adaptedFixture.Channels.Add(new Channel { Name = "Intensiteit", Type = "Dimmer" });
                adaptedFixture.Channels.Add(new Channel { Name = "Rood (CH-1)", Type = "RGB" });
                adaptedFixture.Channels.Add(new Channel { Name = "Groen (CH-2)", Type = "RGB" });
                adaptedFixture.Channels.Add(new Channel { Name = "Blauw (CH-3)", Type = "RGB" });
            }

            SelectedFixture = adaptedFixture;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Closes the dialog window
        }

        // Effect model
        public class EffectRow
        {
            public string EffectType { get; set; } = "Fade-in";
            public string Min { get; set; } = "";
            public string Max { get; set; } = "";
            public string Time { get; set; } = "";
            public object Channel { get; set; }
        }

        // Collection for effect rows
        public ObservableCollection<EffectRow> EffectRows { get; set; } = new();

        // Call this method from your button click event in XAML
        private void AddEffectRow(object sender, RoutedEventArgs e)
        {
            EffectRows.Add(new EffectRow());
        }
    }
}
