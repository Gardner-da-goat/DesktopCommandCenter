using System.Collections.ObjectModel;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class MacrosViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly FavoritesViewModel _favorites;
    private readonly WindowsViewModel _windows;
    private readonly ShellActionService _shellActions;
    private readonly CommandsViewModel _commands;

    private string _newMacroName = string.Empty;
    private string _newMacroScript = string.Empty;
    private string _statusMessage = "Ready";
    private bool _isRunning;

    public MacrosViewModel(
        AppSettings settings,
        ISettingsService settingsService,
        FavoritesViewModel favorites,
        WindowsViewModel windows,
        ShellActionService shellActions,
        CommandsViewModel commands)
    {
        _settings = settings;
        _settingsService = settingsService;
        _favorites = favorites;
        _windows = windows;
        _shellActions = shellActions;
        _commands = commands;

        Items = new ObservableCollection<MacroItemViewModel>();
        foreach (var macro in _settings.Macros)
        {
            AddViewModel(macro);
        }

        CreateCommand = new RelayCommand(CreateMacro);
    }

    public ObservableCollection<MacroItemViewModel> Items { get; }
    public RelayCommand CreateCommand { get; }

    public string NewMacroName
    {
        get => _newMacroName;
        set => SetProperty(ref _newMacroName, value);
    }

    public string NewMacroScript
    {
        get => _newMacroScript;
        set => SetProperty(ref _newMacroScript, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasMacros => Items.Count > 0;
    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    private void CreateMacro()
    {
        var name = NewMacroName.Trim();
        var script = NewMacroScript.Trim();

        if (name.Length == 0 || script.Length == 0)
        {
            StatusMessage = "Enter a macro name and at least one command.";
            return;
        }

        var macro = new MacroSetting
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Script = script
        };

        _settings.Macros.Add(macro);
        AddViewModel(macro);
        Save();
        NewMacroName = string.Empty;
        NewMacroScript = string.Empty;
        StatusMessage = $"Created {name}.";
    }

    private void AddViewModel(MacroSetting macro)
    {
        MacroItemViewModel? item = null;
        item = new MacroItemViewModel(
            macro,
            new RelayCommand(() => _ = RunAsync(macro)),
            new RelayCommand(() =>
            {
                if (item is not null)
                {
                    Remove(item);
                }
            }));

        Items.Add(item);
    }

    private void Remove(MacroItemViewModel item)
    {
        _settings.Macros.Remove(item.Setting);
        Items.Remove(item);
        Save();
        StatusMessage = $"Removed {item.Name}.";
    }

    private async Task RunAsync(MacroSetting macro)
    {
        if (IsRunning)
        {
            StatusMessage = "Another macro is already running.";
            return;
        }

        IsRunning = true;
        StatusMessage = $"Running {macro.Name}…";

        try
        {
            var lines = macro.Script.Split(
                ['\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                if (!await ExecuteLineAsync(line))
                {
                    StatusMessage = $"Stopped on unsupported or unavailable command: {line}";
                    return;
                }
            }

            StatusMessage = $"Finished {macro.Name}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Macro stopped: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task<bool> ExecuteLineAsync(string line)
    {
        if (line.StartsWith("favorite ", StringComparison.OrdinalIgnoreCase))
        {
            var name = line["favorite ".Length..].Trim();
            var favorite = _favorites.Items.FirstOrDefault(item =>
                item.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

            if (favorite is null)
            {
                return false;
            }

            favorite.LaunchCommand.Execute(null);
            return true;
        }

        if (line.StartsWith("command ", StringComparison.OrdinalIgnoreCase))
        {
            var name = line["command ".Length..].Trim();
            var command = _commands.Items.FirstOrDefault(item =>
                item.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));

            if (command is null)
            {
                return false;
            }

            command.RunCommand.Execute(null);
            return true;
        }

        if (line.StartsWith("delay ", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(line["delay ".Length..].Trim(), out var milliseconds))
            {
                return false;
            }

            await Task.Delay(Math.Clamp(milliseconds, 0, 10_000));
            return true;
        }

        if (line.Equals("downloads", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.OpenDownloads();
        }

        if (line.Equals("taskmanager", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.OpenTaskManager();
        }

        if (line.Equals("settings", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.OpenWindowsSettings();
        }

        if (line.Equals("terminal", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.OpenTerminal();
        }

        if (line.Equals("screenshot", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.OpenScreenshot();
        }

        if (line.Equals("mute", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.ToggleMute();
        }

        if (line.Equals("volume-up", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.VolumeUp();
        }

        if (line.Equals("volume-down", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.VolumeDown();
        }

        if (line.Equals("clipboard", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.OpenClipboardHistory();
        }

        if (line.Equals("play-pause", StringComparison.OrdinalIgnoreCase))
        {
            return _shellActions.PlayPause();
        }

        _windows.Refresh();
        var current = _windows.CurrentWindow;
        if (current is null)
        {
            return false;
        }

        if (line.Equals("snap-left", StringComparison.OrdinalIgnoreCase))
        {
            current.SnapLeftCommand.Execute(null);
            return true;
        }

        if (line.Equals("snap-right", StringComparison.OrdinalIgnoreCase))
        {
            current.SnapRightCommand.Execute(null);
            return true;
        }

        if (line.Equals("center", StringComparison.OrdinalIgnoreCase))
        {
            current.CenterCommand.Execute(null);
            return true;
        }

        if (line.Equals("minimize", StringComparison.OrdinalIgnoreCase))
        {
            current.MinimizeCommand.Execute(null);
            return true;
        }

        if (line.Equals("maximize", StringComparison.OrdinalIgnoreCase))
        {
            current.MaximizeCommand.Execute(null);
            return true;
        }

        if (line.StartsWith("opacity ", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(line["opacity ".Length..].Trim(), out var opacity))
        {
            current.Opacity = Math.Clamp(opacity, 20, 100);
            return true;
        }

        if (line.StartsWith("top ", StringComparison.OrdinalIgnoreCase))
        {
            var value = line["top ".Length..].Trim();
            if (value.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                current.IsAlwaysOnTop = true;
                return true;
            }

            if (value.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                current.IsAlwaysOnTop = false;
                return true;
            }
        }

        return false;
    }

    private void Save()
    {
        _settingsService.Save(_settings);
        OnPropertyChanged(nameof(HasMacros));
    }
}
