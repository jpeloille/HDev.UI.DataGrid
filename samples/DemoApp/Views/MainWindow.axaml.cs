using Avalonia.Controls;
using DemoApp.ViewModels;

namespace DemoApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
