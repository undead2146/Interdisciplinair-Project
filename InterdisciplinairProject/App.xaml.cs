using System.IO;
using System.Windows;

namespace InterdiscplinairProject;

/// <summary>
/// Interaction logic for <see cref="App.xaml"/>.
/// <remarks>
/// This class represents the application entry point for the InterdisciplinairProject WPF application.
/// It inherits from <see cref="Application"/> and handles the application's lifecycle,
/// including startup via <see cref="StartupUri" /> in XAML, resource management, and shutdown events.
/// The class is partial to allow XAML code-behind integration. Detailed initialization occurs in
/// <see cref="OnStartup(StartupEventArgs)" /> if overridden, but defaults to loading
/// <see cref="MainWindow" />.
/// </remarks>
/// <seealso cref="Application" />
/// <seealso cref="MainWindow" />
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // map 'data' aanmaken
        string dataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        if (!Directory.Exists(dataDir)) 
        {
            Directory.CreateDirectory(dataDir);
        }
    }
}
