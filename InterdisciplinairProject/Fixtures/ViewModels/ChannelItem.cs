using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models.Enums;
using System;
using System.Collections.Generic;
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
        [ObservableProperty] private string? selectedRangeType;
        [ObservableProperty] private int level;
        [ObservableProperty] private int maxValue = 255;
        [ObservableProperty] private int minValue = 0;

        [ObservableProperty] private Dictionary<string, ChannelRange> ranges = new();

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

        private bool _isNameManuallyEdited;


        private readonly Channel _model;

        public ChannelItem(Channel model)
        {
            _model = model;

            var available = TypeCatalogService.Names;

            // Name: if empty → give default
            if (string.IsNullOrWhiteSpace(_model.Name))
                name = "Channel";
            else
                name = _model.Name;

            // If model.Type is null/empty OR not in the known list, fall back to Dimmer
            if (string.IsNullOrWhiteSpace(_model.Type) || !available.Contains(_model.Type))
                selectedType = "Dimmer";
            else
                selectedType = _model.Type!;

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

            // 🔹 Merge this channel's min/max + ranges into the TYPE definition
            if (!string.IsNullOrWhiteSpace(_model.Type))
            {
                var spec = TypeCatalogService.GetByName(_model.Type);
                if (spec == null)
                {
                    spec = new TypeSpecification
                    {
                        name = _model.Type,
                        input = "slider",
                        min = _model.Min,   // if 0 it’s fine
                        max = _model.Max == 0 ? 255 : _model.Max
                    };
                }

                // Merge ranges from channel into spec
                if (_model.Ranges != null && _model.Ranges.Count > 0)
                {
                    spec.ranges ??= new Dictionary<string, ChannelRange>();
                    foreach (var kv in _model.Ranges)
                    {
                        spec.ranges[kv.Key] = kv.Value;
                    }
                }

                TypeCatalogService.AddOrUpdate(spec);
            }

            // 🔹 Also keep a local copy for this channel's UI
            if (_model.Ranges != null)
                ranges = new Dictionary<string, ChannelRange>(_model.Ranges);
            else
                ranges = new Dictionary<string, ChannelRange>();

            // Apply spec (will set MinValue/MaxValue and copy spec.ranges into Ranges)
            ApplyTypeSpec(selectedType);

            AddCustomTypeCommand = new RelayCommand(DoAddCustomType);
            AddCustomRangeCommand = new RelayCommand(DoAddCustomRange);
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
                    _model.Ranges = new Dictionary<string, ChannelRange>(spec.ranges);
                else
                    _model.Ranges = new Dictionary<string, ChannelRange>();
            }
            else
            {
                // Fallback: use the current channel values
                _model.Min = MinValue;
                _model.Max = MaxValue;
                _model.Ranges = Ranges != null
                    ? new Dictionary<string, ChannelRange>(Ranges)
                    : new Dictionary<string, ChannelRange>();
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
            // Base type we’re adding a range to
            var baseType = SelectedRangeType ?? SelectedType;

            if (string.IsNullOrWhiteSpace(baseType))
            {
                MessageBox.Show("Select a base type first.");
                return;
            }

            var rangeName = (CustomRangeName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(rangeName))
            {
                MessageBox.Show("Range name is empty.");
                return;
            }

            if (!int.TryParse(CustomRangeMinValue, out var rmin))
            {
                MessageBox.Show("Range min is not a valid number.");
                return;
            }

            if (!int.TryParse(CustomRangeMaxValue, out var rmax))
            {
                MessageBox.Show("Range max is not a valid number.");
                return;
            }

            if (rmin < MinValue || rmax > MaxValue || rmin >= rmax)
            {
                MessageBox.Show(
                    $"Range must be within [{MinValue}, {MaxValue}] and min < max.",
                    "Invalid range",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 🔹 Get the TYPE we’re adding ranges to
            var spec = TypeCatalogService.GetByName(baseType);
            if (spec == null)
            {
                MessageBox.Show($"Base type '{baseType}' not found.");
                return;
            }

            spec.ranges ??= new Dictionary<string, ChannelRange>();

            if (spec.ranges.ContainsKey(rangeName))
            {
                MessageBox.Show("A range with this name already exists for this type.");
                return;
            }

            spec.ranges[rangeName] = new ChannelRange
            {
                MinR = rmin,
                MaxR = rmax
            };

            // 🔹 Write back into catalog (so all channels using this type see it)
            TypeCatalogService.AddOrUpdate(spec);

            // 🔹 Sync this channel’s local Ranges from the updated spec
            Ranges = new Dictionary<string, ChannelRange>(spec.ranges);

            // Optionally ensure min/max match the type
            MinValue = spec.min ?? MinValue;
            MaxValue = spec.max ?? MaxValue;

            // Clear UI fields
            CustomRangeName = string.Empty;
            CustomRangeMinValue = string.Empty;
            CustomRangeMaxValue = string.Empty;

            MessageBox.Show($"Range '{rangeName}' added for type '{baseType}'.");
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
            if (spec.ranges != null)
                Ranges = new Dictionary<string, ChannelRange>(spec.ranges);
            else
                Ranges = new Dictionary<string, ChannelRange>();

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

