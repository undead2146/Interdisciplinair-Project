using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

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

        // Effect properties
        [ObservableProperty] private bool effectEnabled;
        [ObservableProperty] private EffectType effectType;
        [ObservableProperty] private int effectTime;
        [ObservableProperty] private byte effectMin;
        [ObservableProperty] private byte effectMax;
        [ObservableProperty] private ChannelEffect? selectedEffect;

        // Custom type panel
        [ObservableProperty] private string? customTypeName;
        [ObservableProperty] private int customTypeSliderValue;
        [ObservableProperty] private string? customRangeName;
        [ObservableProperty] private string customRangeMinValue = string.Empty;
        [ObservableProperty] private string customRangeMaxValue = string.Empty;

        [ObservableProperty] private string typeMinValue = string.Empty;
        [ObservableProperty] private string typeMaxValue = string.Empty;
        [ObservableProperty] private bool isRangeTabEnabled = true;

        // Type flags
        [ObservableProperty] private bool isSliderType;
        [ObservableProperty] private bool isCustomType;
        [ObservableProperty] private bool isAddRangeType;
        [ObservableProperty] private bool isDegreeHType;
        [ObservableProperty] private bool isDegreeFType;

        public ObservableCollection<ChannelEffect> Effects { get; } = new();
        public int TickFrequency => 1;
        public IReadOnlyList<string> AvailableTypes => TypeCatalogService.Names;
        public IEnumerable<EffectType> AvailableEffects { get; } = Enum.GetValues(typeof(EffectType)).Cast<EffectType>();

        public IRelayCommand AddCustomTypeCommand { get; }
        public IRelayCommand AddCustomRangeCommand { get; }
        public IRelayCommand AddRangeCommand { get; }
        public IRelayCommand AddEffectCommand { get; }
        public IRelayCommand<ChannelEffect> DeleteEffectCommand { get; }

        private bool _changeLock = false;
        private readonly Channel _model;

        public ChannelItem(Channel model)
        {
            _model = model;

            var available = TypeCatalogService.Names;

            name = string.IsNullOrWhiteSpace(_model.Name) ? "Channel" : _model.Name;

            selectedType = string.IsNullOrWhiteSpace(_model.Type) || !available.Contains(_model.Type)
                ? "Select a type"
                : _model.Type!;

            if (int.TryParse(_model.Value, out var lvl))
                level = lvl;
            else
                level = 0;

            if (_model.ChannelEffect != null && _model.ChannelEffects.Any())
            {
                foreach (var e in _model.ChannelEffects)
                    Effects.Add(e);

                SelectedEffect = Effects.FirstOrDefault();
            }

            Ranges = _model.Ranges != null
                ? new ObservableCollection<ChannelRange>(_model.Ranges)
                : new ObservableCollection<ChannelRange>();

            var spec = TypeCatalogService.GetByName(selectedType);
            if (spec != null)
            {
                MinValue = spec.min ?? 0;
                MaxValue = spec.max ?? 255;
                if (ranges.Count == 0 && spec.ranges != null)
                    ranges = new ObservableCollection<ChannelRange>(spec.ranges);
            }

            AddCustomTypeCommand = new RelayCommand(DoAddCustomType);
            AddCustomRangeCommand = new RelayCommand(DoAddCustomRange);
            AddRangeCommand = new RelayCommand(AddRange);
            AddEffectCommand = new RelayCommand(AddEffect);
            DeleteEffectCommand = new RelayCommand<ChannelEffect>(DeleteEffect);
        }
        partial void OnNameChanged(string? oldValue, string newValue)
        {
            bool _isNameManuallyEdited = true;
            foreach (var type in TypeCatalogService.Names)
            {
                if (newValue == type)
                {
                    _isNameManuallyEdited = false;
                }
            }
            if (_isNameManuallyEdited)
            {
                _changeLock = true;
            }
        }

        partial void OnSelectedTypeChanged(string oldValue, string newValue)
        {
            ApplyTypeSpec(newValue);

            if (Name == oldValue.ToString() && !_changeLock)
            {
                Name = newValue;
            }

            Level = Snap(Level, MinValue, MaxValue);
            TypeMinValue = MinValue.ToString();
            TypeMaxValue = MaxValue.ToString();
        }

        private void AddEffect()
        {
            var newEffect = new ChannelEffect { EffectType = EffectType.FadeIn };
            Effects.Add(newEffect);
            SelectedEffect = newEffect;
        }

        private void DeleteEffect(ChannelEffect? effect)
        {
            if (effect != null && Effects.Contains(effect))
                Effects.Remove(effect);
        }

        private void AddRange()
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

            var spec = TypeCatalogService.GetByName(_model.Type);

            if (spec != null)
            {
                _model.Min = spec.min ?? 0;
                _model.Max = spec.max ?? 255;
                _model.Ranges = spec.ranges != null ? new List<ChannelRange>(Ranges) : new List<ChannelRange>();
            }
            else
            {
                _model.Min = MinValue;
                _model.Max = MaxValue;
                _model.Ranges = new List<ChannelRange>(Ranges);
            }

            if (Effects.Any())
            {
                _model.ChannelEffect = Effects.First();
                _model.ChannelEffects = Effects.ToList();
            }
            else
            {
                _model.ChannelEffect = new ChannelEffect();
                _model.ChannelEffects = new List<ChannelEffect>();
            }

            return _model;
        }

        private void DoAddCustomType()
        {
            var typeName = (CustomTypeName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(typeName))
            {
                MessageBox.Show("Type name is empty.");
                return;
            }

            if (string.Equals(typeName, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Choose another name than 'Custom'.");
                return;
            }

            //if (!int.TryParse(CustomRangeMinValue, out var min) || !int.TryParse(CustomRangeMaxValue, out var max))
            //{
            //    MessageBox.Show("Invalid min/max values.");
            //    return;
            //}

            //if (min < 0 || max > 99999 || min >= max)
            //{
            //    MessageBox.Show("Min must be >=0, max <=99999, and min < max.");
            //    return;
            //}

            var spec = new TypeSpecification
            {
                name = typeName,
                input = "slider",
                min = 0,
                max = 255
                //min = min,
                //max = max
            };

            if (!TypeCatalogService.AddOrUpdate(spec))
            {
                MessageBox.Show("Failed to save type.");
                return;
            }

            OnPropertyChanged(nameof(AvailableTypes));

            SelectedType = typeName;
            Level = Snap(Level, MinValue, MaxValue);
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

            if (!int.TryParse(CustomRangeMinValue, out var rmin) || !int.TryParse(CustomRangeMaxValue, out var rmax))
            {
                MessageBox.Show("Range min/max are not valid numbers.");
                return;
            }

            if (rmin < MinValue || rmax > MaxValue || rmin >= rmax)
            {
                MessageBox.Show($"Range must be within [{MinValue},{MaxValue}] and min<max.");
                return;
            }

            if (ranges.Any(r => r.Name.Equals(rangeName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A range with this name already exists.");
                return;
            }

            var newRange = new ChannelRange { Name = rangeName, MinR = rmin, MaxR = rmax };
            ranges.Add(newRange);
            _model.Ranges = ranges.ToList();

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
            if (spec == null)
            {
                IsRangeTabEnabled = false;
                return;
            }

            IsRangeTabEnabled = !spec.input.Equals("noInput", StringComparison.OrdinalIgnoreCase)
                                && !spec.input.Equals("custom", StringComparison.OrdinalIgnoreCase);

            if (spec.input.Equals("slider", StringComparison.OrdinalIgnoreCase))
            {
                IsSliderType = true;
                MinValue = spec.min ?? 0;
                MaxValue = spec.max ?? 255;
                TypeMinValue = MinValue.ToString();
                TypeMaxValue = MaxValue.ToString();
            }
            else if (spec.input.Equals("rangeToType", StringComparison.OrdinalIgnoreCase))
            {
                IsAddRangeType = true;
            }
            else if (spec.input.Equals("custom", StringComparison.OrdinalIgnoreCase))
            {
                IsCustomType = true;
                IsRangeTabEnabled = false;
            }

            Ranges = spec.ranges != null ? new ObservableCollection<ChannelRange>(spec.ranges) : new ObservableCollection<ChannelRange>();
        }

        private static int Snap(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        partial void OnSelectedRangeTypeChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var spec = TypeCatalogService.GetByName(value);
            if (spec == null) return;

            MinValue = spec.min ?? 0;
            MaxValue = spec.max ?? 255;

            CustomRangeMinValue = MinValue.ToString();
            CustomRangeMaxValue = MaxValue.ToString();
        }
    }
}
