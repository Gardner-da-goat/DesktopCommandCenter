using System.Windows.Input;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class FavoriteItemViewModel
{
    public FavoriteItemViewModel(
        string name,
        string launchPath,
        ICommand launchCommand,
        ICommand removeCommand)
    {
        Name = name;
        LaunchPath = launchPath;
        LaunchCommand = launchCommand;
        RemoveCommand = removeCommand;
    }

    public string Name { get; }
    public string LaunchPath { get; }
    public ICommand LaunchCommand { get; }
    public ICommand RemoveCommand { get; }
}
