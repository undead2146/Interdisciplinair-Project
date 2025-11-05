using InterdisciplinairProject.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using InterdisciplinairProject.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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
    public static IServiceProvider Services { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // 🔹 Hier registreer je al je services en viewmodels
        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        // Start de hoofdwindow via DI
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // 🧠 ViewModels
        services.AddSingleton<MainViewModel>();     // singleton ViewModel
        services.AddSingleton<ShowbuilderViewModel>();     // singleton ViewModel

        // 🪟 Views
        services.AddTransient<MainWindow>();
    }
}
