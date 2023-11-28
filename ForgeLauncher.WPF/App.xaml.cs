using CommunityToolkit.Mvvm.DependencyInjection;
using ForgeLauncher.WPF.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace ForgeLauncher.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceProvider = ConfigureServices();
            Ioc.Default.ConfigureServices(serviceProvider);

            var mainWindow = Ioc.Default.GetService<MainWindow>()!;
            mainWindow.ShowDialog();
        }

        private IServiceProvider ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();
            foreach(var type in Assembly.GetAssembly(typeof(App))?.GetTypes() ?? Enumerable.Empty<Type>())
            {
                var exportAttributes = type.GetCustomAttributes<ExportAttribute>();
                if (exportAttributes != null)
                {
                    var isSingleton = type.GetCustomAttribute<SharedAttribute>() != null;
                    foreach (var exportAttribute in exportAttributes)
                    {
                        var contractType = exportAttribute.ContractType ?? type;
                        if (isSingleton)
                            serviceCollection.AddSingleton(contractType, type);
                        else
                            serviceCollection.AddTransient(contractType, type);
                    }
                }
            }

            return serviceCollection.BuildServiceProvider();
        }
    }
}
