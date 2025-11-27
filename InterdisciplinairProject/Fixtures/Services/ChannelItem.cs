using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InterdisciplinairProject.Fixtures.Models;
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

        private readonly Channel _model;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool isExpanded;

        public ChannelItem(Channel model)
        {
            _model = model;

            Name = _model.Name;
            SelectedType = string.IsNullOrWhiteSpace(_model.Type) ? "Lamp" : _model.Type!;
            if (int.TryParse(_model.Value, out var lvl)) Level = lvl;

            TypeCatalogService.EnsureLoaded();
            ApplyTypeSpec(SelectedType);

            AddCustomTypeCommand = new RelayCommand(DoAddCustomType);
        }
        private bool _isNameManuallyEdited = false;

        private string _name = "Lamp";
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    _model.Name = value;

                    // mark as manually edited if it's different from the type
                    _isNameManuallyEdited = value != _selectedType;
                }
            }
        }

        private string _selectedType = "Lamp";
        public string SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value))
                {
                    _model.Type = value;
                    ApplyTypeSpec(value);
                    if (IsSliderType) Level = Level; // re-snap

                    // update Name only if the user hasn't manually edited it
                    if (!_isNameManuallyEdited)
                        Name = value;
                }
            }
        }


        private int _maxValue = 255;
        public int MaxValue
        {
            get => _maxValue;
            set
            {
                if (SetProperty(ref _maxValue, value))
                {
                    OnPropertyChanged(nameof(TickFrequency));
                    if (Level > _maxValue)
                        Level = _maxValue;
                }
            }
        }


        // Slider level <-> model.Value
        private int _level;
        public int Level
        {
            get => _level;
            set
            {
                var snapped = Snap(value, Math.Max(1, SliderDivisions), MaxValue);
                if (SetProperty(ref _level, snapped))
                    _model.Value = snapped.ToString();
            }
        }

        public IReadOnlyList<string> AvailableTypes => TypeCatalogService.Names;

        private bool _isSliderType;
        public bool IsSliderType
        {
            get => _isSliderType;
            set => SetProperty(ref _isSliderType, value);
        }

        private bool _isCustomType;
        public bool IsCustomType
        {
            get => _isCustomType;
            set => SetProperty(ref _isCustomType, value);
        }

        private bool _isAddRangeType;
        public bool IsAddRangeType
        {
            get => _isAddRangeType;
            set => SetProperty(ref _isAddRangeType, value);
        }

        private bool _isDegreeHType;
        public bool IsDegreeHType
        {
            get => _isDegreeHType;
            set => SetProperty(ref _isDegreeHType, value);
        }
        private bool _isDegreeFType;
        public bool IsDegreeFType
        {
            get => _isDegreeFType;
            set => SetProperty(ref _isDegreeFType, value);
        }

        private int _sliderDivisions = 255;
        public int SliderDivisions
        {
            get => _sliderDivisions;
            set
            {
                if (SetProperty(ref _sliderDivisions, value))
                    OnPropertyChanged(nameof(TickFrequency));
            }
        }

        public int TickFrequency => Math.Max(1, MaxValue / Math.Max(1, SliderDivisions));


        // Custom panel fields
        private string? _customTypeName;
        public string? CustomTypeName
        {
            get => _customTypeName;
            set => SetProperty(ref _customTypeName, value);
        }

        private int _customTypeSliderValue;
        public int CustomTypeSliderValue
        {
            get => _customTypeSliderValue;
            set => SetProperty(ref _customTypeSliderValue, value);
        }

        private string? _customRangeName;
        public string? CustomRangeName
        {
            get => _customRangeName;
            set => SetProperty(ref _customRangeName, value);
        }

        private string _customRangeMinValue;
        public string CustomRangeMinValue
        {
            get => _customRangeMinValue;
            set => SetProperty(ref _customRangeMinValue, value);
        }
        private string _customRangeMaxValue;
        public string CustomRangeMaxValue
        {
            get => _customRangeMaxValue;
            set => SetProperty(ref _customRangeMaxValue, value);
        }

        public IRelayCommand AddCustomTypeCommand { get; }

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

