using InterdisciplinairProject.ViewModels;
using Show.Model;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
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
                return;

            // If the DataContext provided by the ItemsControl is a Scene model,
            // replace it with a SceneControlViewModel that wraps the model and
            // has a reference to the parent ShowbuilderViewModel.
            if (DataContext is Scene sceneModel)
            {
                var parentShowVm = FindParentShowbuilderViewModel();
                this.DataContext = new SceneControlViewModel(sceneModel, parentShowVm);
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
