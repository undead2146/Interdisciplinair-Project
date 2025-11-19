using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace InterdisciplinairProject.Views;

/// <summary>
/// Interaction logic for SceneView.xaml
/// </summary>
public partial class SceneView : UserControl
{
    public SceneView()
    {
        InitializeComponent();
        // DataContext = new SceneViewModel(); // hook up when ready
    }
}
