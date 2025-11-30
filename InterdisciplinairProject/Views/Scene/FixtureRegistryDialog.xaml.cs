using InterdisciplinairProject.Core.Interfaces;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;

namespace InterdisciplinairProject.Views.Scene
{
    /// <summary>
    /// Interaction logic for FixtureRegistryDialog.xaml
    /// </summary>
    public partial class FixtureRegistryDialog : Window
    {
        private readonly IDmxAddressValidator? _dmxAddressValidator;
        private readonly IFixtureRegistry? _fixtureRegistry;
        private int _startAddress = 1;
        private string _fixtureName = string.Empty;
        private IEnumerable<Fixture>? _existingFixtures = Enumerable.Empty<Fixture>();

        /// <summary>
        /// Gets the available effect types from the EffectType enum.
        /// </summary>
        public Array EffectTypes => Enum.GetValues(typeof(EffectType));

        public InterdisciplinairProject.Core.Models.Fixture SelectedFixture
        {
            get { return (InterdisciplinairProject.Core.Models.Fixture)GetValue(SelectedFixtureProperty); }
            set { SetValue(SelectedFixtureProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedFixture.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedFixtureProperty =
            DependencyProperty.Register("SelectedFixture", typeof(InterdisciplinairProject.Core.Models.Fixture), typeof(FixtureRegistryDialog), new PropertyMetadata(null));

        public int StartAddress
        {
            get { return _startAddress; }
            set
            {
                if (_startAddress != value)
                {
                    _startAddress = value;
                    OnPropertyChanged(nameof(StartAddress));
                    OnPropertyChanged(nameof(EndAddress));
                }
            }
        }

        public int EndAddress
        {
            get { return _startAddress + (SelectedFixture?.Channels.Count ?? 0) - 1; }
        }

        public string FixtureName
        {
            get { return _fixtureName; }
            set
            {
                if (_fixtureName != value)
                {
                    _fixtureName = value;
                    OnPropertyChanged(nameof(FixtureName));
                }
            }
        }

        public FixtureRegistryDialog()
        {
            InitializeComponent();
            this.DataContext = this; // Set DataContext for direct binding in XAML
        }

        /// <summary>
        /// Constructor overload to accept Core.Models.Fixture
        /// </summary>
        /// <param name="coreFixture">The core fixture to adapt.</param>
        public FixtureRegistryDialog(InterdisciplinairProject.Core.Models.Fixture coreFixture)
            : this()
        {
            SelectedFixture = coreFixture;

            // Set a default name based on the template fixture
            FixtureName = $"{coreFixture.Name} - Instance {DateTime.Now:yyyyMMdd_HHmmss}";

            // Add default channels if none exist, using Core.Models.Channel
            if (SelectedFixture.Channels == null || SelectedFixture.Channels.Count == 0)
            {
                SelectedFixture.Channels = new ObservableCollection<Channel>
                {
                    new Channel { Name = "Intensiteit", Type = "Dimmer", Value = "0", Min = 0, Max = 255 },
                    new Channel { Name = "Rood", Type = "Red", Value = "0", Min = 0, Max = 255 },
                    new Channel { Name = "Groen", Type = "Green", Value = "0", Min = 0, Max = 255 },
                    new Channel { Name = "Blauw", Type = "Blue", Value = "0", Min = 0, Max = 255 }
                };
            }

            // Set initial start address based on core fixture if available
            if (coreFixture.StartAddress > 0)
            {
                StartAddress = coreFixture.StartAddress;
            }
            else
            {
                StartAddress = 1;
            }
        }

        /// <summary>
        /// Constructor overload that accepts a validator, registry, and existing fixtures
        /// </summary>
        public FixtureRegistryDialog(InterdisciplinairProject.Core.Models.Fixture coreFixture,
            IDmxAddressValidator validator,
            IFixtureRegistry fixtureRegistry,
            IEnumerable<Fixture> existingFixtures)
            : this(coreFixture)
        {
            _dmxAddressValidator = validator;
            _fixtureRegistry = fixtureRegistry;
            _existingFixtures = existingFixtures ?? Enumerable.Empty<Fixture>();

            // Calculate the next available address
            int suggestedAddress = _dmxAddressValidator.FindNextAvailableAddress(
                SelectedFixture.Channels.Count,
                _existingFixtures);

            if (suggestedAddress > 0)
            {
                StartAddress = suggestedAddress; // Update start address if a suggestion is found
            }
            // If no suggestion (suggestedAddress <= 0), keep the start address set by the coreFixture or default to 1.
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (SelectedFixture == null)
                {
                    MessageBox.Show("Geen fixture geselecteerd.", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(FixtureName))
                {
                    MessageBox.Show("Voer een naam in voor de fixture.", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (StartAddress < 1 || StartAddress > 512)
                {
                    MessageBox.Show("Start adres moet tussen 1 en 512 zijn.", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create a NEW fixture instance (not modifying the original template)
                var newFixture = new Fixture
                {
                    FixtureId = SelectedFixture.FixtureId,
                    InstanceId = $"{FixtureName.Replace(" ", "_")}_{Guid.NewGuid():N}",
                    Name = FixtureName,
                    Manufacturer = SelectedFixture.Manufacturer,
                    Description = SelectedFixture.Description,
                    StartAddress = StartAddress,
                    Channels = new ObservableCollection<Channel>(),
                    ChannelDescriptions = new Dictionary<string, string>(SelectedFixture.ChannelDescriptions ?? new Dictionary<string, string>()),
                    ChannelTypes = new Dictionary<string, ChannelType>(SelectedFixture.ChannelTypes ?? new Dictionary<string, ChannelType>())
                };

                // Copy channels from template
                foreach (var channel in SelectedFixture.Channels)
                {
                    newFixture.Channels.Add(new Channel
                    {
                        Name = channel.Name,
                        Type = channel.Type,
                        Value = channel.Value,
                        Parameter = channel.Parameter,
                        Min = channel.Min,
                        Max = channel.Max,
                        Time = channel.Time,
                        ChannelEffect = channel.ChannelEffect ?? new ChannelEffect()
                    });
                }

                // Process effect rows and add them to channels
                foreach (var effectRow in EffectRows)
                {
                    // Use SelectedChannel property
                    if (effectRow.SelectedChannel is Channel templateChannel)
                    {
                        // Find the corresponding channel in the new fixture
                        var targetChannel = newFixture.Channels.FirstOrDefault(c => c.Name == templateChannel.Name);

                        if (targetChannel != null)
                        {
                            // Parse and validate effect values
                            if (!byte.TryParse(effectRow.Min, out byte minValue))
                            {
                                MessageBox.Show($"Ongeldige Min waarde voor effect: {effectRow.Min}", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            if (!byte.TryParse(effectRow.Max, out byte maxValue))
                            {
                                MessageBox.Show($"Ongeldige Max waarde voor effect: {effectRow.Max}", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            if (!int.TryParse(effectRow.Time, out int timeValue))
                            {
                                MessageBox.Show($"Ongeldige Time waarde voor effect: {effectRow.Time}", "Validatiefout", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            // Create and assign the effect to the channel
                            targetChannel.ChannelEffect = new ChannelEffect
                            {
                                EffectType = effectRow.EffectType,
                                Min = minValue,
                                Max = maxValue,
                                Time = timeValue
                            };
                        }
                    }
                }

                // Save to registry if available
                if (_fixtureRegistry != null)
                {
                    bool success = await _fixtureRegistry.AddFixtureAsync(newFixture);

                    if (success)
                    {
                        MessageBox.Show($"Fixture '{newFixture.Name}' succesvol opgeslagen in registry!", "Succes", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.DialogResult = true;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show($"Fixture '{newFixture.InstanceId}' bestaat al in de registry.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Fixture registry is niet beschikbaar. Injecteer IFixtureRegistry in de constructor.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fout bij opslaan van fixture: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close(); // Closes the dialog window
        }

        // Effect model with filtering support
        public class EffectRow : INotifyPropertyChanged
        {
            // Holds all channels from the fixture template
            private readonly IEnumerable<Channel> _allChannels;

            // Now represents the single channel selected from the filtered list
            private Channel? _selectedChannel;

            private EffectType _effectType = EffectType.FadeIn;
            private ObservableCollection<Channel> _availableChannels; // List of channels filtered by the selected effect

            // De statische EffectMapping is niet langer nodig voor het filteren, maar we laten de structuur intact.
            private static readonly Dictionary<string, List<EffectType>> EffectMapping =
                new Dictionary<string, List<EffectType>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Dimmer", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse, EffectType.Strobe } },
                    { "Intensity", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse, EffectType.Strobe } },
                    { "Red", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
                    { "Green", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
                    { "Blue", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
                    { "White", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
                    { "Amber", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
                    { "Strobe", new List<EffectType> { EffectType.Strobe, EffectType.Pulse } },
                    { "Pan", new List<EffectType> { EffectType.Custom } },
                    { "Tilt", new List<EffectType> { EffectType.Custom } },
                    { "Gobo", new List<EffectType> { EffectType.Custom } },
                    { "Color", new List<EffectType> { EffectType.Custom } },
                    { "Speed", new List<EffectType> { EffectType.Custom } },
                    { "Lamp", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Strobe, EffectType.Pulse } },
                    { "Star", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Pulse } },
                    { "Custom", new List<EffectType> { EffectType.FadeIn, EffectType.FadeOut, EffectType.Strobe, EffectType.Pulse, EffectType.Custom } },
                };

            // Constructor to receive all channels
            public EffectRow(IEnumerable<Channel> allChannels)
            {
                _allChannels = allChannels;
                // Initial update based on default EffectType.FadeIn
                UpdateAvailableChannels();
            }


            public EffectType EffectType
            {
                get => _effectType;
                set
                {
                    if (_effectType != value)
                    {
                        _effectType = value;
                        OnPropertyChanged(nameof(EffectType));
                        UpdateAvailableChannels(); // <-- Trigger channel filtering when effect changes
                    }
                }
            }

            public string Min { get; set; } = "0";
            public string Max { get; set; } = "255";
            public string Time { get; set; } = "1000";

            // Property for the selected channel
            public Channel? SelectedChannel
            {
                get => _selectedChannel;
                set
                {
                    if (_selectedChannel != value)
                    {
                        _selectedChannel = value;
                        OnPropertyChanged(nameof(SelectedChannel));

                        // Update effect parameters if a channel is selected and it has an existing effect
                        if (_selectedChannel?.ChannelEffect != null)
                        {
                            Min = _selectedChannel.ChannelEffect.Min.ToString();
                            Max = _selectedChannel.ChannelEffect.Max.ToString();
                            Time = _selectedChannel.ChannelEffect.Time.ToString();
                            OnPropertyChanged(nameof(Min));
                            OnPropertyChanged(nameof(Max));
                            OnPropertyChanged(nameof(Time));
                        }
                    }
                }
            }

            /// <summary>
            /// Gets the available channels, filtered by the currently selected EffectType.
            /// </summary>
            public ObservableCollection<Channel> AvailableChannels
            {
                get => _availableChannels ??= new ObservableCollection<Channel>();
                set
                {
                    _availableChannels = value;
                    OnPropertyChanged(nameof(AvailableChannels));
                }
            }

            /// <summary>
            /// Gets the appropriate effects for a channel type (Still defined, but not used for filtering in this mode).
            /// </summary>
            private static List<EffectType> GetEffectsForChannelType(string channelType)
            {
                if (EffectMapping.TryGetValue(channelType, out var effects))
                {
                    return effects;
                }
                return Enum.GetValues(typeof(EffectType)).Cast<EffectType>().ToList();
            }

            // DE BELANGRIJKSTE WIJZIGING: Filtert op basis van bestaande JSON-waarde
            private void UpdateAvailableChannels()
            {
                var filteredChannels = _allChannels
                    .Where(channel =>
                    {
                        // Controleer of het kanaal een bestaand effect heeft.
                        if (channel.ChannelEffect != null)
                        {
                            // Retourneer ALLEEN het kanaal waarvan het bestaande effecttype
                            // overeenkomt met het geselecteerde EffectType in de dropdown.
                            return channel.ChannelEffect.EffectType == EffectType;
                        }

                        // Als het kanaal GEEN bestaand effect heeft, wordt het uitgesloten.
                        return false;
                    })
                    .ToList();

                // Update de ObservableCollection
                AvailableChannels = new ObservableCollection<Channel>(filteredChannels);

                // Re-selecteer het kanaal indien nodig.
                if (SelectedChannel == null || !AvailableChannels.Contains(SelectedChannel))
                {
                    SelectedChannel = AvailableChannels.FirstOrDefault();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //Collection for effect rows
        public ObservableCollection<EffectRow> EffectRows { get; set; } = new();

        //// Call this method from your button click event in XAML
        private void AddEffectRow(object sender, RoutedEventArgs e)
        {
            if (SelectedFixture?.Channels != null)
            {
                // Pass the list of all channels to the new EffectRow instance
                EffectRows.Add(new EffectRow(SelectedFixture.Channels.Cast<Channel>()));
            }
            else
            {
                // Fallback for an empty or unselected fixture
                EffectRows.Add(new EffectRow(Enumerable.Empty<Channel>()));
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}