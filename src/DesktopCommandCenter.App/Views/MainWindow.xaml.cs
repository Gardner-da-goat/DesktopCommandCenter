using System.ComponentModel;
using System.Windows;
using DesktopCommandCenter.App.ViewModels;

namespace DesktopCommandCenter.App.Views;

public partial class MainWindow : Window
{
    private bool _closingForExit;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closing += OnClosing;
    }

    public void CloseForExit()
    {
        _closingForExit = true;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closingForExit)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }
}
