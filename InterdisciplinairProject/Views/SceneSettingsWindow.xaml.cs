using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace InterdisciplinairProject.Views;

/// <summary>
/// Interaction logic for SceneView.xaml
/// </summary>
public partial class SceneView : UserControl
{
    /// <summary>
    /// Interaction logic for SceneSettingsWindow.xaml
    /// </summary>
    public partial class SceneSettingsWindow : Window
    {
        public SceneSettingsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
