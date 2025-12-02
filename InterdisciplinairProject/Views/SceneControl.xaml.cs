using InterdisciplinairProject.ViewModels;
using InterdisciplinairProject.Core.Models;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Reflection;

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

            // Handle clicks on the control body so clicking anywhere selects the scene.
            PreviewMouseLeftButtonDown += SceneControl_PreviewMouseLeftButtonDown;
        }

        private void SceneControl_Loaded(object? sender, RoutedEventArgs e)
        {
            if (DataContext is InterdisciplinairProject.Core.Models.Scene sceneModel)
            {
                var parentShowVm = FindParentShowbuilderViewModel();
                this.DataContext = new SceneControlViewModel(sceneModel, parentShowVm);
            }
        }

        // Select the scene when clicking the control body (but ignore clicks that originate on interactive children).
        private void SceneControl_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (e?.OriginalSource is not DependencyObject src)
                return;

            var parentShowVm = FindParentShowbuilderViewModel();
            if (parentShowVm == null) return;

            if (DataContext is SceneControlViewModel vm && vm.SceneModel != null)
            {
                parentShowVm.SelectedScene = vm.SceneModel;
                ExecuteSceneSelectionCommand(parentShowVm, vm.SceneModel);
            }
        }

        private static void ExecuteSceneSelectionCommand(ShowbuilderViewModel parentShowVm, InterdisciplinairProject.Core.Models.Scene scene)
        {
            if (parentShowVm == null || scene == null) return;

            try
            {
                var cmdProp = parentShowVm.GetType().GetProperty("SceneSelectionChangedCommand", BindingFlags.Public | BindingFlags.Instance);
                if (cmdProp != null)
                {
                    var cmdVal = cmdProp.GetValue(parentShowVm);
                    if (cmdVal is System.Windows.Input.ICommand cmd && cmd.CanExecute(scene))
                    {
                        cmd.Execute(scene);
                        return;
                    }
                }

                // Fallback: call the method (may be private) via reflection
                var method = parentShowVm.GetType().GetMethod("SceneSelectionChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(parentShowVm, new object[] { scene });
                }
            }
            catch
            {
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
