using InterdisciplinairProject.Core.Models;
using InterdisciplinairProject.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InterdisciplinairProject.Views
{
    public partial class TimelineScene : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineScene"/> class.
        /// </summary>
        public TimelineScene()
        {
            InitializeComponent();
            Loaded += SceneControl_Loaded;
        }

        public TimelineShowScene TimelineSceneModel
        {
            get { return (TimelineShowScene) GetValue(SceneModelProperty); }
            set { SetValue(SceneModelProperty, value); }
        }

        public static readonly DependencyProperty SceneModelProperty =
            DependencyProperty.Register(
                "sceneModel",
                typeof(TimelineShowScene),
                typeof(TimelineScene),
                new PropertyMetadata(null));

        private void SceneControl_Loaded(object? sender, RoutedEventArgs e)
        {
            // If the DataContext provided by the ItemsControl is a ShowScene model,
            // replace it with a TimeLineViewModel that wraps the model and
            // has a reference to the parent ShowbuilderViewModel.
            if (DataContext is TimelineShowScene sceneModel)
            {
                var parentShowVm = FindParentShowbuilderViewModel();
                this.DataContext = new TimeLineViewModel(sceneModel, parentShowVm);
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
