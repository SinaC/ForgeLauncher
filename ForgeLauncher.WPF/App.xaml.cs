using CommunityToolkit.Mvvm.DependencyInjection;
using ForgeLauncher.WPF.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;
using System.Windows;

namespace ForgeLauncher.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string LogFile = "Forge Launcher.WPF.log";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ILogger logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(LogFile, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, rollingInterval: RollingInterval.Infinite, rollOnFileSizeLimit: false)
            .CreateLogger();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddExportedTypesFromAssembly(Assembly.GetAssembly(typeof(App)));
        serviceCollection.AddSingleton(logger);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        Ioc.Default.ConfigureServices(serviceProvider);

        var mainWindow = Ioc.Default.GetService<MainWindow>()!;
        mainWindow.ShowDialog();
    }
}
