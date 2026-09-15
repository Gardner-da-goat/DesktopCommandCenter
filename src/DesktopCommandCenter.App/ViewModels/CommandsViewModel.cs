using System.Collections.ObjectModel;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class CommandsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly ISettingsService _settingsService;
    private readonly ShellActionService _shellActions;
    private string _newCommandName = string.Empty;
    private string _newCommandTarget = string.Empty;
    private string _statusMessage = "Ready";

    public CommandsViewModel(
        AppSettings settings,
        ISettingsService settingsService,
        ShellActionService shellActions)
    {
        _settings = settings;
        _settingsService = settingsService;
        _shellActions = shellActions;
        Items = new ObservableCollection<CommandItemViewModel>();

        foreach (var command in _settings.CustomCommands)
        {
            AddViewModel(command);
        }

        CreateCommand = new RelayCommand(Create);
    }

    public ObservableCollection<CommandItemViewModel> Items { get; }
    public RelayCommand CreateCommand { get; }

    public string NewCommandName
    {
        get => _newCommandName;
        set => SetProperty(ref _newCommandName, value);
    }

    public string NewCommandTarget
    {
        get => _newCommandTarget;
        set => SetProperty(ref _newCommandTarget, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool HasCommands => Items.Count > 0;

    private void Create()
    {
        var name = NewCommandName.Trim();
        var target = NewCommandTarget.Trim();

        if (name.Length == 0 || target.Length == 0)
        {
            StatusMessage = "Enter a name and target.";
            return;
        }

        if (_settings.CustomCommands.Any(command =>
                command.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)))
        {
            StatusMessage = "A command with that name already exists.";
            return;
        }

        var setting = new CustomCommandSetting
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Target = target
        };

        _settings.CustomCommands.Add(setting);
        AddViewModel(setting);
        Save();

        NewCommandName = string.Empty;
        NewCommandTarget = string.Empty;
        StatusMessage = $"Added {name}.";
    }

    private void AddViewModel(CustomCommandSetting setting)
    {
        CommandItemViewModel? item = null;
        item = new CommandItemViewModel(
            setting,
            new RelayCommand(() =>
            {
                StatusMessage = _shellActions.OpenPath(setting.Target)
                    ? $"Opened {setting.Name}."
                    : $"Could not open {setting.Name}.";
            }),
            new RelayCommand(() =>
            {
                if (item is not null)
                {
                    Remove(item);
                }
            }));

        Items.Add(item);
    }

    private void Remove(CommandItemViewModel item)
    {
        _settings.CustomCommands.Remove(item.Setting);
        Items.Remove(item);
        Save();
        StatusMessage = $"Removed {item.Name}.";
    }

    private void Save()
    {
        _settingsService.Save(_settings);
        OnPropertyChanged(nameof(HasCommands));
    }
}
