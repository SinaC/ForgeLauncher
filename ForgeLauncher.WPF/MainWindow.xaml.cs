using ForgeLauncher.WPF.Attributes;
using System.Reflection;
using System.Windows;

namespace ForgeLauncher.WPF;

[Export]
public partial class MainWindow : Window
{
    public MainWindow(MainVM mainVM)
    {
        InitializeComponent();

        DataContext = mainVM;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
            Title = $"Forge Launcher - {version.Major}.{version.Minor}.{version.Build}";
    }
}
