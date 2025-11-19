using System.Windows;

namespace InterdisciplinairProject.Views;

/// <summary>
/// Interaction logic for SceneSettingsWindow.xaml.
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
