using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        [ObservableProperty] private int level;
        [ObservableProperty] private int maxValue = 255;

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

        // Slider divisions
        [ObservableProperty] private int sliderDivisions = 255;

        public int TickFrequency => Math.Max(1, MaxValue / Math.Max(1, SliderDivisions));

        public IReadOnlyList<string> AvailableTypes => TypeCatalogService.Names;

        public IEnumerable<EffectType> AvailableEffects { get; } = Enum.GetValues(typeof(EffectType)).Cast<EffectType>();

        public IRelayCommand AddCustomTypeCommand { get; }

        private bool _isNameManuallyEdited;


        private readonly Channel _model;

        public ChannelItem(Channel model)
        {
            _model = model;

            // Init from model
            name = _model.Name;
            selectedType = string.IsNullOrWhiteSpace(_model.Type) ? "Lamp" : _model.Type!;
            if (int.TryParse(_model.Value, out var lvl)) level = lvl;

            // Effect init
            if (_model.ChannelEffect != null)
            {
                effectEnabled = _model.ChannelEffect.Enabled;
                effectType = _model.ChannelEffect.EffectType;
                effectTime = _model.ChannelEffect.Time;
                effectMin = _model.ChannelEffect.Min;
                effectMax = _model.ChannelEffect.Max;
            }

            TypeCatalogService.EnsureLoaded();
            ApplyTypeSpec(selectedType);

            AddCustomTypeCommand = new RelayCommand(DoAddCustomType);
        }

        // Sync back to model when saving
        public Channel ToModel()
        {
            _model.Name = Name;
            _model.Type = SelectedType;
            _model.Value = Level.ToString();

            _model.ChannelEffect.EffectType = EffectType;
            _model.ChannelEffect.Time = EffectTime;
            _model.ChannelEffect.Min = EffectMin;
            _model.ChannelEffect.Max = EffectMax;

            return _model;
        }

        private void DoAddCustomType()
        {

            MessageBox.Show($"{CustomRangeName}: max:{CustomRangeMaxValue}  min: {CustomRangeMinValue}");
            var name = (CustomTypeName ?? "").Trim();
            var divisions = CustomTypeSliderValue;

            if (string.IsNullOrWhiteSpace(name)) { MessageBox.Show("Type name is empty."); return; }
            if (divisions <= 0 || divisions > 255) { MessageBox.Show("Step value must be between 1 and 255."); return; }
            if (string.Equals(name, "Custom", StringComparison.OrdinalIgnoreCase)) { MessageBox.Show("Choose another name than 'Custom'."); return; }

            var spec = new TypeSpecification { name = name, input = "slider", divisions = divisions };
            if (!TypeCatalogService.AddOrUpdate(spec)) { MessageBox.Show("Failed to save the type."); return; }

            OnPropertyChanged(nameof(AvailableTypes)); // refresh ComboBox
            SelectedType = name;                       // becomes slider with divisions
            IsCustomType = false;                      // hide custom panel
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
                MaxValue = 255;

                SliderDivisions = spec.divisions.GetValueOrDefault(255);
            }
            if (spec.input.Equals("degreeH", StringComparison.OrdinalIgnoreCase))
            {
                IsDegreeHType = true;
                MaxValue = 180;

                SliderDivisions = spec.divisions.GetValueOrDefault(180);
            }
            if (spec.input.Equals("degreeF", StringComparison.OrdinalIgnoreCase))
            {
                IsDegreeFType = true;

                MaxValue = 360;
                SliderDivisions = spec.divisions.GetValueOrDefault(360);
            }
            if (spec.input.Equals("rangeToType", StringComparison.OrdinalIgnoreCase))
            {
                IsAddRangeType = true;
            }
            else if (spec.input.Equals("custom", StringComparison.OrdinalIgnoreCase))
            {
                IsCustomType = true;
            }
            // "text" -> panels remain collapsed by XAML triggers
        }

        private static int Snap(int value, int divisions, int max)
        {
            var step = Math.Max(1, max / Math.Max(1, divisions));
            var snapped = (int)Math.Round((double)value / step) * step;
            return Math.Max(0, Math.Min(max, snapped));
        }

    }

}

