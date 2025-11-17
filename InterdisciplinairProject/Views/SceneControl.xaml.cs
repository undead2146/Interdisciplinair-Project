using InterdisciplinairProject.ViewModels;
using Show.Model;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace InterdisciplinairProject.Views
{
    public partial class SceneControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneControl"/> class.
        /// </summary>
        public SceneControl()
        {
            InitializeComponent();
            Loaded += SceneControl_Loaded;
        }

        private void SceneControl_Loaded(object? sender, RoutedEventArgs e)
        {
            // If the DataContext is already a SceneControlViewModel, do nothing.
            if (DataContext is SceneControlViewModel)
            {
                AttachSliderHandlers();
                return;
            }

            // If the DataContext provided by the ItemsControl is a Scene model,
            // replace it with a SceneControlViewModel that wraps the model and
            // has a reference to the parent ShowbuilderViewModel.
            if (DataContext is Scene sceneModel)
            {
                var parentShowVm = FindParentShowbuilderViewModel();
                this.DataContext = new SceneControlViewModel(sceneModel, parentShowVm);
            }

            AttachSliderHandlers();
        }

        private void AttachSliderHandlers()
        {
            var slider = this.FindName("PART_DimmerSlider") as Slider;
            if (slider != null)
            {
                // when user presses the slider, select this scene so fixture details show
                slider.PreviewMouseLeftButtonDown -= Slider_PreviewMouseLeftButtonDown;
                slider.PreviewMouseLeftButtonDown += Slider_PreviewMouseLeftButtonDown;

                // also handle keyboard focus/clicks
                slider.PreviewMouseDown -= Slider_PreviewMouseLeftButtonDown;
                slider.PreviewMouseDown += Slider_PreviewMouseLeftButtonDown;
            }
        }

        private void Slider_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            var parentShowVm = FindParentShowbuilderViewModel();
            if (parentShowVm == null) return;

            if (DataContext is SceneControlViewModel vm && vm.SceneModel != null)
            {
                parentShowVm.SelectedScene = vm.SceneModel;
            }
        }

        private ShowbuilderViewModel? FindParentShowbuilderViewModel()
        {
            DependencyObject? current = this;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is ShowbuilderViewModel sbvm)
                    return sbvm;

                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
