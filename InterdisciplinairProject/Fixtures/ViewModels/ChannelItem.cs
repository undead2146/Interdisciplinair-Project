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
        [ObservableProperty] private int maxValue = 255;
        [ObservableProperty] private int minValue = 0;
        [ObservableProperty] private ObservableCollection<ChannelRange> ranges = new();

        // Effect properties
        [ObservableProperty] private ChannelEffect? selectedEffect;

        // Custom type panel
        [ObservableProperty] private string? customTypeName;

        [ObservableProperty] private string typeMinValue = string.Empty;
        [ObservableProperty] private string typeMaxValue = string.Empty;

        [ObservableProperty] private string typeMinValue = string.Empty;
        [ObservableProperty] private string typeMaxValue = string.Empty;
        [ObservableProperty] private bool isRangeTabEnabled = true;

        // Type flags
        [ObservableProperty] private bool isCustomType;

        public ObservableCollection<ChannelEffect> Effects { get; } = new();
        public IReadOnlyList<string> AvailableTypes => TypeCatalogService.Names;
        public IEnumerable<EffectType> AvailableEffects { get; } = Enum.GetValues(typeof(EffectType)).Cast<EffectType>();

        public IRelayCommand AddCustomTypeCommand { get; }
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

            // ✅ VALIDATE ranges (least changes: validate only on Save)
            foreach (var r in Ranges)
            {
                // max may never exceed 255
                if (r.MaxR > 255)
                {
                    MessageBox.Show($"Range '{r.Name}': Max cannot exceed 255.",
                                    "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new InvalidOperationException("Range Max > 255");
                }

                // min must be <= max
                if (r.MinR > r.MaxR)
                {
                    MessageBox.Show($"Range '{r.Name}': Min cannot be bigger than Max.",
                                    "Validation error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new InvalidOperationException("Range Min > Max");
                }
            }

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

            var spec = new TypeSpecification
            {
                name = typeName,
                input = "slider",
                min = 0,
                max = 255
            };

            if (!TypeCatalogService.AddOrUpdate(spec))
            {
                MessageBox.Show("Failed to save type.");
                return;
            }

            OnPropertyChanged(nameof(AvailableTypes));

            SelectedType = typeName;
            IsCustomType = false;
        }

        

        private void ApplyTypeSpec(string? typeName)
        {
            IsCustomType = false;


            var spec = TypeCatalogService.GetByName(typeName);
            if (spec == null)
            {
                return;
            }
            if (spec.input.Equals("slider", StringComparison.OrdinalIgnoreCase))
            {
                MinValue = spec.min ?? 0;
                MaxValue = spec.max ?? 255;
                TypeMinValue = MinValue.ToString();
                TypeMaxValue = MaxValue.ToString();
            }
            else if (spec.input.Equals("custom", StringComparison.OrdinalIgnoreCase))
            {
                IsCustomType = true;
                IsRangeTabEnabled = false;
            }

            Ranges = spec.ranges != null ? new ObservableCollection<ChannelRange>(spec.ranges) : new ObservableCollection<ChannelRange>();
        }
    }
}
