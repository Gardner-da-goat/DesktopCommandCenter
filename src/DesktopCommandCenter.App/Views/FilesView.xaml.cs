using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopCommandCenter.App.ViewModels;

namespace DesktopCommandCenter.App.Views;

public partial class FilesView : System.Windows.Controls.UserControl
{
    public FilesView()
    {
        InitializeComponent();
    }

    private void OnFilesDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FilesViewModel viewModel)
        {
            viewModel.OpenItem(FilesList.SelectedItem as FileExplorerItem);
        }
    }

    private void OnQuickLocationClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is FilesViewModel viewModel &&
            sender is System.Windows.Controls.Button { Tag: FileExplorerItem item })
        {
            viewModel.NavigateQuick(item);
        }
    }
}
