using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls.Primitives;

namespace InterdisciplinairProject.Views
{
    public partial class SceneControl : UserControl
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            nameof(IsActive), typeof(bool), typeof(SceneControl), new PropertyMetadata(false, OnIsActiveChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SceneControl control)
            {
                control.UpdateEnabledState();
            }
        }

        public SceneControl()
        {
            InitializeComponent();
            Loaded += SceneControl_Loaded;
        }

        private void SceneControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.FindName("PART_DimmerSlider") is Slider s)
            {
                s.ValueChanged -= DimmerSlider_ValueChanged;
                s.ValueChanged += DimmerSlider_ValueChanged;
            }

            if (this.FindName("PART_TopToggle") is ToggleButton tb)
            {
                tb.Click -= TopToggle_Click;
                tb.Click += TopToggle_Click;

                // prevent toggling red->green via mouse or keyboard
                tb.PreviewMouseLeftButtonDown -= TopToggle_PreviewMouseLeftButtonDown;
                tb.PreviewMouseLeftButtonDown += TopToggle_PreviewMouseLeftButtonDown;
                tb.PreviewKeyDown -= TopToggle_PreviewKeyDown;
                tb.PreviewKeyDown += TopToggle_PreviewKeyDown;
            }

            UpdateEnabledState();
        }

        private void DimmerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // LED follows the slider strictly: green when > 0, red at 0
            IsActive = e.NewValue > 0.0;
        }

        private void TopToggle_Click(object? sender, RoutedEventArgs e)
        {
            // allow green -> red by clicking the LED: set slider to 0
            if (!IsActive && this.FindName("PART_DimmerSlider") is Slider s && s.Value == 0)
            {
                // already off, nothing to do
                return;
            }

            if (this.FindName("PART_DimmerSlider") is Slider slider)
            {
                // when clicked while active (green), turn off
                if (IsActive && slider.Value > 0)
                {
                    slider.Value = 0;
                }
            }
        }

        private void TopToggle_PreviewMouseLeftButtonDown(object? sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // block red -> green via mouse click
            if (!IsActive)
            {
                e.Handled = true;
            }
        }

        private void TopToggle_PreviewKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
        {
            // block red -> green via keyboard (Space/Enter)
            if (!IsActive && (e.Key == System.Windows.Input.Key.Space || e.Key == System.Windows.Input.Key.Enter))
            {
                e.Handled = true;
            }
        }

        private void UpdateEnabledState()
        {
            // ensure slider remains enabled; the LED purely reflects slider value
            if (this.FindName("PART_DimmerSlider") is Slider s)
            {
                s.IsEnabled = true;
            }

            // always keep the yellow gradient (no grey background)
            if (this.FindName("PART_Fill") is Border fill && this.FindName("PART_FillGradient") is LinearGradientBrush grad)
            {
                fill.Background = grad;
            }
        }
    }
}
