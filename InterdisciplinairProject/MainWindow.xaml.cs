using System.Diagnostics;
using System.Windows;
using InterdisciplinairProject.ViewModels;
using InterdisciplinairProject.Views;

namespace InterdiscplinairProject;

/// <summary>
/// Interaction logic for <see cref="MainWindow.xaml" />.
/// <remarks>
/// This class represents the main window of the InterdisciplinairProject WPF application.
/// It inherits from <see cref="Window" /> and serves as the primary user interface container.
/// The window is initialized via <see cref="InitializeComponent()" />, which loads XAML resources
/// and sets up event handlers. DataContext is set to <see cref="MainViewModel" /> for MVVM binding.
/// Custom logic for DMX lighting controls (e.g., fixture management, scene playback) will be handled
/// in ViewModels from the Features projects, bound via XAML.
/// Dimensions are set in XAML (Height=450, Width=800), and Title is bound to ViewModel property.
/// </remarks>
/// <seealso cref="App" />
/// <seealso cref="MainViewModel" />
/// <seealso cref="Window" />
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow" /> class.
    /// <remarks>
    /// Initializes the component and sets the DataContext to a new instance of MainViewModel.
    /// This enables data binding for MVVM architecture.
    /// </remarks>
    /// <seealso cref="InitializeComponent()" />
    /// <seealso cref="MainViewModel" />
    /// </summary>
    public MainWindow()
    {
        Debug.WriteLine("[DEBUG] MainWindow constructor called");
        Console.WriteLine("[DEBUG] MainWindow constructor called");
        InitializeComponent();
        Debug.WriteLine("[DEBUG] µMainWindow InitializeComponent completed");
        Console.WriteLine("[DEBUG] MainWindow InitializeComponent completed");

        DataContext = new MainViewModel();
        Debug.WriteLine("[DEBUG] MainWindow DataContext set to MainViewModel");
        Console.WriteLine("[DEBUG] MainWindow DataContext set to MainViewModel");
    }
    private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm) { 
            if(vm.CurrentView is ShowbuilderView)
            {

                MessageBoxResult result = MessageBox.Show(
                    "Do you want to save the changes?",
                    "Confirm",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    vm.SaveCloseForShow();

                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }

    }
}
