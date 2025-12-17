using InterdisciplinairProject.ViewModels.Scene;
using System.Windows;
using System.Windows.Input;

namespace InterdisciplinairProject.Views.Scene;

/// <summary>
/// Interaction logic for FixtureRegistryDialog.xaml.
/// </summary>
public partial class FixtureRegistryDialog : Window
{
    private FixtureRegistryImportViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixtureRegistryDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    public FixtureRegistryDialog(FixtureRegistryImportViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void AddEffectRow(object sender, RoutedEventArgs e)
    {
        _viewModel.AddEffectRow();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.Save();
        if (_viewModel is not null) // Assuming Save sets some success flag
        {
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Cancel();
        DialogResult = false;
        Close();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }
}