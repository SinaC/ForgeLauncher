using ForgeLauncher.WPF.Attributes;
using System.Windows;

namespace ForgeLauncher.WPF;

[Export]
public partial class MainWindow : Window
{
    public MainWindow(MainVM mainVM)
    {
        InitializeComponent();

        DataContext = mainVM;
    }
}
