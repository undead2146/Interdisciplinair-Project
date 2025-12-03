using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace InterdisciplinairProject.Fixtures.Services
{
    public partial class ChannelItem : ObservableObject
    {
        // UI state
        [ObservableProperty] private bool isEditing;
        [ObservableProperty] private bool isExpanded;

        // Core channel properties
        [ObservableProperty] private string name;
        [ObservableProperty] private string selectedType;
        [ObservableProperty] private ChannelRange? selectedRange;
        [ObservableProperty] private string? selectedRangeType;
        [ObservableProperty] private int level;
        [ObservableProperty] private int maxValue = 255;
        [ObservableProperty] private int minValue = 0;

        [ObservableProperty] private ObservableCollection<ChannelRange> ranges = new();

        // Effect properties (map to ChannelEffect)
        [ObservableProperty] private bool effectEnabled;
        [ObservableProperty] private EffectType effectType;
        [ObservableProperty] private int effectTime;
        [ObservableProperty] private byte effectMin;
        [ObservableProperty] private byte effectMax;

        // Custom type panel
        [ObservableProperty] private string? customTypeName;
        [ObservableProperty] private int customTypeSliderValue;
        [ObservableProperty] private string? customRangeName;
        [ObservableProperty] private string customRangeMinValue = string.Empty;
        [ObservableProperty] private string customRangeMaxValue = string.Empty;

        // Type flags
        [ObservableProperty] private bool isSliderType;
        [ObservableProperty] private bool isCustomType;
        [ObservableProperty] private bool isAddRangeType;
        [ObservableProperty] private bool isDegreeHType;
        [ObservableProperty] private bool isDegreeFType;

        public int TickFrequency => 1;

        public IReadOnlyList<string> AvailableTypes => TypeCatalogService.Names;

        public IEnumerable<EffectType> AvailableEffects { get; } = Enum.GetValues(typeof(EffectType)).Cast<EffectType>();

        public IRelayCommand AddCustomTypeCommand { get; }

        public IRelayCommand AddCustomRangeCommand { get; }

        public IRelayCommand AddRangeCommand { get; }

        private bool _isNameManuallyEdited;

        private readonly Channel _model;

        public ChannelItem(Channel model)
        {
            _model = model;

            var available = TypeCatalogService.Names;

            name = string.IsNullOrWhiteSpace(_model.Name) ? "Channel" : _model.Name;

            // Name: if empty → give default
            selectedType = string.IsNullOrWhiteSpace(_model.Type) || !available.Contains(_model.Type)
                ? "Dimmer"
                : _model.Type!;

            // Value
            if (int.TryParse(_model.Value, out var lvl))
                level = lvl;
            else
                level = 0;

            // Effect init
            if (_model.ChannelEffect != null)
            {
                effectEnabled = _model.ChannelEffect.Enabled;
                effectType = _model.ChannelEffect.EffectType;
                effectTime = _model.ChannelEffect.Time;
                effectMin = _model.ChannelEffect.Min;
                effectMax = _model.ChannelEffect.Max;
            }

            // 🔹 Keep a local copy for this channel's UI
            ranges = _model.Ranges != null
                ? new ObservableCollection<ChannelRange>(_model.Ranges)
                : new ObservableCollection<ChannelRange>();

            // 🔹 Apply defaults from type spec (min/max), maar ranges niet terugschrijven
            var spec = TypeCatalogService.GetByName(selectedType);
            if (spec != null)
            {
                MinValue = spec.min ?? 0;
                MaxValue = spec.max ?? 255;
                // eventueel: ranges alleen gebruiken als er nog geen lokale ranges zijn
                if (ranges.Count == 0 && spec.ranges != null)
                    ranges = new ObservableCollection<ChannelRange>(spec.ranges);
            }


            AddCustomTypeCommand = new RelayCommand(DoAddCustomType);
            AddCustomRangeCommand = new RelayCommand(DoAddCustomRange);
            AddRangeCommand = new RelayCommand(DoAddRange);
        }

        // Sync back to model when saving
        //public Channel ToModel()
        //{
        //    _model.Name = Name;
        //    _model.Type = SelectedType;
        //    _model.Value = Level.ToString();
        //    _model.Min = MinValue;
        //    _model.Max = MaxValue;

        //    _model.ChannelEffect.EffectType = EffectType;
        //    _model.ChannelEffect.Time = EffectTime;
        //    _model.ChannelEffect.Min = EffectMin;
        //    _model.ChannelEffect.Max = EffectMax;

        //    // 🔹 Write ranges to model → ends up in JSON
        //    _model.Ranges = Ranges != null
        //        ? new Dictionary<string, ChannelRange>(Ranges)
        //        : new Dictionary<string, ChannelRange>();

        //    ApplyTypeSpec(_model.Type);
        //    return _model;
        //}

        private void DoAddRange()
        {
            var newRange = new ChannelRange
            {
                Name = $"Range{Ranges.Count + 1}",
                MinR = MinValue,
                MaxR = MaxValue
            };
            Ranges.Add(newRange);
            SelectedRange = newRange;
            _model.Ranges = Ranges.ToList();
        }

        public Channel ToModel()
        {
            _model.Name = Name;
            _model.Type = SelectedType;
            _model.Value = Level.ToString();

            // 🔹 Get spec for this type
            var spec = TypeCatalogService.GetByName(_model.Type);

            if (spec != null)
            {
                _model.Min = spec.min ?? 0;
                _model.Max = spec.max ?? 255;

                if (spec.ranges != null)
                    _model.Ranges = new List<ChannelRange>(Ranges);
                else
                    _model.Ranges = new List<ChannelRange>();
            }
            else
            {
                // Fallback: use the current channel values
                _model.Min = MinValue;
                _model.Max = MaxValue;
                _model.Ranges = Ranges != null
                    ? new List<ChannelRange>(Ranges)
                    : new List<ChannelRange>();
            }

            // Effect stuff
            if (_model.ChannelEffect == null)
                _model.ChannelEffect = new ChannelEffect();

            _model.ChannelEffect.Enabled = EffectEnabled;
            _model.ChannelEffect.EffectType = EffectType;
            _model.ChannelEffect.Time = EffectTime;
            _model.ChannelEffect.Min = EffectMin;
            _model.ChannelEffect.Max = EffectMax;

            return _model;
        }


        private void DoAddCustomType()
        {
            var name = (CustomTypeName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Type name is empty.");
                return;
            }

            if (string.Equals(name, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Choose another name than 'Custom'.");
                return;
            }

            // Parse min/max from the textboxes
            if (!int.TryParse(CustomRangeMinValue, out var min))
            {
                MessageBox.Show("Min value is not a valid number.");
                return;
            }

            if (!int.TryParse(CustomRangeMaxValue, out var max))
            {
                MessageBox.Show("Max value is not a valid number.");
                return;
            }

            if (min < 0 || max > 255 || min >= max)
            {
                MessageBox.Show("Min must be >= 0, max <= 255 and min < max.");
                return;
            }

            // NOW we use the user-specified min/max for the new type
            var spec = new TypeSpecification
            {
                name = name,
                input = "slider",
                min = min,
                max = max
            };

            if (!TypeCatalogService.AddOrUpdate(spec))
            {
                MessageBox.Show("Failed to save the type.");
                return;
            }

            // Refresh combobox items
            OnPropertyChanged(nameof(AvailableTypes));

            // Select the new type -> triggers OnSelectedTypeChanged -> ApplyTypeSpec(name)
            SelectedType = name;

            // Optional: snap current value into range
            Level = Snap(Level, MinValue, MaxValue);

            // Hide custom panel
            IsCustomType = false;
        }

        private void DoAddCustomRange()
        {
            var rangeName = (CustomRangeName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(rangeName))
            {
                MessageBox.Show("Range name is empty.");
                return;
            }

            if (!int.TryParse(CustomRangeMinValue, out var rmin) ||
                !int.TryParse(CustomRangeMaxValue, out var rmax))
            {
                MessageBox.Show("Range min/max are not valid numbers.");
                return;
            }

            if (rmin < MinValue || rmax > MaxValue || rmin >= rmax)
            {
                MessageBox.Show($"Range must be within [{MinValue}, {MaxValue}] and min < max.");
                return;
            }

            if (ranges.Any(r => r.Name.Equals(rangeName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A range with this name already exists.");
                return;
            }

            var newRange = new ChannelRange
            {
                Name = rangeName,
                MinR = rmin,
                MaxR = rmax
            };

            ranges.Add(newRange);

            // Sync naar model
            _model.Ranges = ranges.ToList();

            // Clear UI fields
            CustomRangeName = string.Empty;
            CustomRangeMinValue = string.Empty;
            CustomRangeMaxValue = string.Empty;

            MessageBox.Show($"Range '{rangeName}' added.");
        }




        private void ApplyTypeSpec(string? typeName)
        {
            IsSliderType = false;
            IsCustomType = false;
            IsDegreeHType = false;
            IsDegreeFType = false;
            IsAddRangeType = false;

            var spec = TypeCatalogService.GetByName(typeName);
            if (spec == null) return;

            if (spec.input.Equals("slider", StringComparison.OrdinalIgnoreCase))
            {
                IsSliderType = true;

                // Use type-defined min/max; default to 0..255 if not set
                MinValue = spec.min ?? 0;
                MaxValue = spec.max ?? 255;
            }
            else if (spec.input.Equals("rangeToType", StringComparison.OrdinalIgnoreCase))
            {
                IsAddRangeType = true;
            }
            else if (spec.input.Equals("custom", StringComparison.OrdinalIgnoreCase))
            {
                IsCustomType = true;
            }

            // 🔹 SYNC ranges from type-spec into THIS channel
            ranges = spec.ranges != null
                ? new ObservableCollection<ChannelRange>(spec.ranges)
                : new ObservableCollection<ChannelRange>();

            // degreeH/degreeF are legacy; if you still want them, you can also map them to min/max here
        }

        private static int Snap(int value, int min, int max)
        {
            // No divisions, just clamp to [min,max]
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        partial void OnSelectedTypeChanged(string value)
        {
            ApplyTypeSpec(value);
            // Clamp current level into new range
            Level = Snap(Level, MinValue, MaxValue);
        }

        partial void OnSelectedRangeTypeChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            // Get the spec of the selected base type (Dimmer, Red, ...)
            var spec = TypeCatalogService.GetByName(value);
            if (spec == null)
                return;

            // Use that type's min/max as the base for ranges
            MinValue = spec.min ?? 0;
            MaxValue = spec.max ?? 255;

            // Prefill the range textboxes with that full span,
            // so user can narrow it down.
            CustomRangeMinValue = MinValue.ToString();
            CustomRangeMaxValue = MaxValue.ToString();
        }

    }

}