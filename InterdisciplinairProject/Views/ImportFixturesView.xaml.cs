using System.Diagnostics;
using System.Windows;
using InterdisciplinairProject.ViewModels;

namespace InterdisciplinairProject.Views;

/// <summary>
/// Interaction logic for ImportFixturesView.xaml.
/// </summary>
public partial class ImportFixturesView : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportFixturesView"/> class.
    /// </summary>
    public ImportFixturesView()
    {
        Debug.WriteLine("[DEBUG] ImportFixturesView constructor called");
        InitializeComponent();
        DataContext = new ImportFixturesViewModel();
        ViewModel.CloseRequested += (s, e) =>
        {
            DialogResult = true;
            Close();
        };
        Debug.WriteLine("[DEBUG] ImportFixturesView initialized");
    }

    /// <summary>
    /// Gets the view model.
    /// </summary>
    public ImportFixturesViewModel ViewModel => (ImportFixturesViewModel)DataContext;

    // AddSelectedFixturesCommand will handle selection logic. The window remains open to allow multiple adds.
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("[DEBUG] Cancel button clicked");
        DialogResult = false;
        Close();
    }
}
