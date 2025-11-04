using System.IO;
using System.Windows;

namespace InterdisciplinairProject;

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
    /// <summary>
    /// Called when the application starts.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialiseer data directories
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InterdisciplinairProject");

        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        // Kopieer fixtures.json als deze nog niet bestaat
        string fixturesDestPath = Path.Combine(appDataDir, "fixtures.json");
        if (!File.Exists(fixturesDestPath))
        {
            // Probeer het uit verschillende locaties te kopiëren
            string[] possibleSourcePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "fixtures.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "InterdisciplinairProject.Features", "Scene", "data", "fixtures.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "InterdisciplinairProject.Features", "Scene", "data", "fixtures.json"),
            };

            foreach (var sourcePath in possibleSourcePaths)
            {
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, fixturesDestPath);
                    break;
                }
            }

            // Als geen bestand gevonden, maak een leeg fixtures bestand aan
            if (!File.Exists(fixturesDestPath))
            {
                File.WriteAllText(fixturesDestPath, "{\"fixtures\":[]}");
            }
        }
    }
}