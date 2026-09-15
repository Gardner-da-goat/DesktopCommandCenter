using System.Windows.Input;
using DesktopCommandCenter.Core.Settings;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class MacroItemViewModel
{
    internal MacroItemViewModel(
        MacroSetting setting,
        ICommand runCommand,
        ICommand removeCommand)
    {
        Setting = setting;
        RunCommand = runCommand;
        RemoveCommand = removeCommand;
    }

    internal MacroSetting Setting { get; }

    public string Id => Setting.Id;
    public string Name => Setting.Name;
    public string Script => Setting.Script;
    public ICommand RunCommand { get; }
    public ICommand RemoveCommand { get; }
}
