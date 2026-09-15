using System.Windows.Input;
using DesktopCommandCenter.Core.Settings;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class CommandItemViewModel
{
    internal CommandItemViewModel(
        CustomCommandSetting setting,
        ICommand runCommand,
        ICommand removeCommand)
    {
        Setting = setting;
        RunCommand = runCommand;
        RemoveCommand = removeCommand;
    }

    internal CustomCommandSetting Setting { get; }
    public string Id => Setting.Id;
    public string Name => Setting.Name;
    public string Target => Setting.Target;
    public ICommand RunCommand { get; }
    public ICommand RemoveCommand { get; }
}
