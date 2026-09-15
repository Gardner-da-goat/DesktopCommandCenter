using System.Collections.ObjectModel;
using DesktopCommandCenter.Windows.Apps;
using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class SearchViewModel : ObservableObject
{
    private readonly WindowsViewModel _windowsViewModel;
    private readonly AppLauncherService _appLauncher;
    private readonly ShellActionService _shellActions;
    private readonly FavoritesViewModel _favorites;
    private readonly MacrosViewModel _macros;
    private readonly CommandsViewModel _commands;
    private readonly SettingsViewModel _settings;
    private IReadOnlyList<InstalledAppInfo>? _installedApps;
    private string _query = string.Empty;

    public SearchViewModel(
        WindowsViewModel windowsViewModel,
        AppLauncherService appLauncher,
        ShellActionService shellActions,
        FavoritesViewModel favorites,
        MacrosViewModel macros,
        CommandsViewModel commands,
        SettingsViewModel settings)
    {
        _windowsViewModel = windowsViewModel;
        _appLauncher = appLauncher;
        _shellActions = shellActions;
        _favorites = favorites;
        _macros = macros;
        _commands = commands;
        _settings = settings;
        _settings.SettingsChanged += OnSettingsChanged;
        Results = new ObservableCollection<SearchResultViewModel>();
    }

    public ObservableCollection<SearchResultViewModel> Results { get; }

    public string Query
    {
        get => _query;
        set
        {
            if (!SetProperty(ref _query, value))
            {
                return;
            }

            RefreshResults();
            NotifySearchState();
        }
    }

    public bool HasQuery => !string.IsNullOrWhiteSpace(Query);
    public bool HasResults => Results.Count > 0;
    public bool HasNoResults => HasQuery && !HasResults;

    private void RefreshResults()
    {
        Results.Clear();

        var query = Query.Trim();
        if (query.Length == 0)
        {
            return;
        }

        if (_settings.SearchWindowsEnabled)
        {
            _windowsViewModel.Refresh();
        }

        if (_settings.SearchAppsEnabled)
        {
            _installedApps ??= _appLauncher.GetInstalledApps();
        }

        var candidates = new List<(int Score, SearchResultViewModel Result)>();

        if (_settings.SearchActionsEnabled)
        {
            AddDirectCommandCandidates(candidates, query);
        }

        if (_settings.SearchWindowsEnabled)
        foreach (var window in _windowsViewModel.Windows)
        {
            var score = MatchScore(query, window.Title, window.ProcessName);
            if (score <= 0)
            {
                continue;
            }

            candidates.Add((
                score + 15,
                new SearchResultViewModel(
                    SearchResultKind.Window,
                    window.Title,
                    $"Window · {window.ProcessName}",
                    "▣",
                    new RelayCommand(() =>
                    {
                        window.ActivateCommand.Execute(null);
                        Query = string.Empty;
                    }))));
        }

        if (_settings.SearchAppsEnabled)
        foreach (var app in _installedApps ?? [])
        {
            var score = MatchScore(query, app.Name);
            if (score <= 0)
            {
                continue;
            }

            candidates.Add((
                score,
                new SearchResultViewModel(
                    SearchResultKind.App,
                    app.Name,
                    "App",
                    "◈",
                    new RelayCommand(() =>
                    {
                        _ = _appLauncher.Launch(app);
                        Query = string.Empty;
                    }),
                    "Pin",
                    new RelayCommand(() => _ = _favorites.AddFavorite(app)))));
        }

        if (_settings.SearchMacrosEnabled)
        foreach (var macro in _macros.Items)
        {
            var score = MatchScore(query, macro.Name, macro.Script);
            if (score <= 0)
            {
                continue;
            }

            candidates.Add((
                score + 12,
                new SearchResultViewModel(
                    SearchResultKind.Macro,
                    macro.Name,
                    "Macro",
                    "▶",
                    new RelayCommand(() =>
                    {
                        macro.RunCommand.Execute(null);
                        Query = string.Empty;
                    }))));
        }

        if (_settings.SearchActionsEnabled)
        {
            foreach (var command in _commands.Items)
            {
                var score = MatchScore(query, command.Name, command.Target);
                if (score > 0)
                {
                    candidates.Add((
                        score + 18,
                        new SearchResultViewModel(
                            SearchResultKind.Action,
                            command.Name,
                            $"Command · {command.Target}",
                            "›",
                            new RelayCommand(() =>
                            {
                                command.RunCommand.Execute(null);
                                Query = string.Empty;
                            }))));
                }
            }

            AddActionCandidate(candidates, query, "Downloads", "Open your Downloads folder", "⇩", _shellActions.OpenDownloads);
            AddActionCandidate(candidates, query, "Task Manager", "Open Task Manager", "▤", _shellActions.OpenTaskManager);
            AddActionCandidate(candidates, query, "Windows Settings", "Open Windows Settings", "⚙", _shellActions.OpenWindowsSettings);
            AddActionCandidate(candidates, query, "Terminal", "Open Windows Terminal", "⌨", _shellActions.OpenTerminal);
            AddActionCandidate(candidates, query, "Screenshot", "Open Windows screen capture", "▧", _shellActions.OpenScreenshot);
            AddActionCandidate(candidates, query, "Mute", "Toggle system mute", "♪", _shellActions.ToggleMute);
            AddActionCandidate(candidates, query, "Volume Up", "Increase system volume", "+", _shellActions.VolumeUp);
            AddActionCandidate(candidates, query, "Volume Down", "Decrease system volume", "−", _shellActions.VolumeDown);
            AddActionCandidate(candidates, query, "Clipboard History", "Open Windows clipboard history", "▣", _shellActions.OpenClipboardHistory);
            AddActionCandidate(candidates, query, "Play Pause", "Toggle media playback", "▶", _shellActions.PlayPause);
        }

        foreach (var result in candidates
                     .OrderByDescending(candidate => candidate.Score)
                     .ThenBy(candidate => candidate.Result.Title, StringComparer.CurrentCultureIgnoreCase)
                     .Take(8)
                     .Select(candidate => candidate.Result))
        {
            Results.Add(result);
        }
    }

    private void AddDirectCommandCandidates(
        ICollection<(int Score, SearchResultViewModel Result)> candidates,
        string query)
    {
        var current = _windowsViewModel.CurrentWindow;

        if (TryGetArgument(query, "web", out var webQuery) && webQuery.Length > 0)
        {
            candidates.Add((
                300,
                new SearchResultViewModel(
                    SearchResultKind.Action,
                    $"Search the web for “{webQuery}”",
                    "Command · default browser",
                    "⌕",
                    new RelayCommand(() =>
                    {
                        _ = _shellActions.SearchWeb(webQuery);
                        Query = string.Empty;
                    }))));
        }

        if (TryGetArgument(query, "open", out var appQuery) && appQuery.Length > 0)
        {
            foreach (var app in (_installedApps ?? [])
                         .Select(app => (App: app, Score: MatchScore(appQuery, app.Name)))
                         .Where(candidate => candidate.Score > 0)
                         .OrderByDescending(candidate => candidate.Score)
                         .Take(3))
            {
                candidates.Add((
                    200 + app.Score,
                    new SearchResultViewModel(
                        SearchResultKind.Action,
                        $"Open {app.App.Name}",
                        "Command · launch app",
                        "▶",
                        new RelayCommand(() =>
                        {
                            _ = _appLauncher.Launch(app.App);
                            Query = string.Empty;
                        }))));
            }
        }

        if ((TryGetArgument(query, "focus", out var windowQuery) ||
             TryGetArgument(query, "activate", out windowQuery)) &&
            windowQuery.Length > 0)
        {
            foreach (var window in FindWindows(windowQuery).Take(3))
            {
                candidates.Add((
                    220,
                    new SearchResultViewModel(
                        SearchResultKind.Action,
                        $"Focus {window.Title}",
                        $"Command · {window.ProcessName}",
                        "▣",
                        new RelayCommand(() =>
                        {
                            window.ActivateCommand.Execute(null);
                            Query = string.Empty;
                        }))));
            }
        }

        if (TryGetArgument(query, "close", out var closeQuery) && closeQuery.Length > 0)
        {
            foreach (var window in FindWindows(closeQuery).Take(3))
            {
                candidates.Add((
                    220,
                    new SearchResultViewModel(
                        SearchResultKind.Action,
                        $"Close {window.Title}",
                        $"Command · asks {window.ProcessName} to close",
                        "×",
                        new RelayCommand(() =>
                        {
                            window.CloseCommand.Execute(null);
                            Query = string.Empty;
                        }))));
            }
        }

        if (TryGetArgument(query, "macro", out var macroQuery) && macroQuery.Length > 0)
        {
            foreach (var macro in _macros.Items
                         .Select(item => (Item: item, Score: MatchScore(macroQuery, item.Name)))
                         .Where(candidate => candidate.Score > 0)
                         .OrderByDescending(candidate => candidate.Score)
                         .Take(3))
            {
                candidates.Add((
                    210 + macro.Score,
                    new SearchResultViewModel(
                        SearchResultKind.Macro,
                        $"Run {macro.Item.Name}",
                        "Command · macro",
                        "▶",
                        new RelayCommand(() =>
                        {
                            macro.Item.RunCommand.Execute(null);
                            Query = string.Empty;
                        }))));
            }
        }

        if (current is null)
        {
            return;
        }

        if (TryGetArgument(query, "opacity", out var opacityText) &&
            int.TryParse(opacityText, out var opacity))
        {
            var clamped = Math.Clamp(opacity, 20, 100);
            AddCurrentWindowCommand(
                candidates,
                $"Set opacity to {clamped}%",
                $"Current window · {current.Title}",
                "◐",
                () => current.Opacity = clamped);
        }

        if (query.Equals("top on", StringComparison.OrdinalIgnoreCase) ||
            query.Equals("always on top", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Turn always-on-top on",
                $"Current window · {current.Title}",
                "↑",
                () => current.IsAlwaysOnTop = true);
        }

        if (query.Equals("top off", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Turn always-on-top off",
                $"Current window · {current.Title}",
                "↓",
                () => current.IsAlwaysOnTop = false);
        }

        if (query.Equals("snap left", StringComparison.OrdinalIgnoreCase) ||
            query.Equals("move left", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Snap current window left",
                current.Title,
                "◧",
                () => current.SnapLeftCommand.Execute(null));
        }

        if (query.Equals("snap right", StringComparison.OrdinalIgnoreCase) ||
            query.Equals("move right", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Snap current window right",
                current.Title,
                "◨",
                () => current.SnapRightCommand.Execute(null));
        }

        if (query.Equals("center", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Center current window",
                current.Title,
                "◎",
                () => current.CenterCommand.Execute(null));
        }

        if (query.Equals("minimize", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Minimize current window",
                current.Title,
                "—",
                () => current.MinimizeCommand.Execute(null));
        }

        if (query.Equals("maximize", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Maximize current window",
                current.Title,
                "□",
                () => current.MaximizeCommand.Execute(null));
        }

        if (query.Equals("restore", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Restore current window",
                current.Title,
                "↙",
                () => current.RestoreCommand.Execute(null));
        }

        if (query.Equals("next monitor", StringComparison.OrdinalIgnoreCase) ||
            query.Equals("move monitor", StringComparison.OrdinalIgnoreCase))
        {
            AddCurrentWindowCommand(
                candidates,
                "Move current window to next monitor",
                current.Title,
                "⇥",
                () => current.MoveToNextMonitorCommand.Execute(null));
        }
    }

    private IEnumerable<WindowItemViewModel> FindWindows(string query) =>
        _windowsViewModel.Windows
            .Select(window => (Window: window, Score: MatchScore(query, window.Title, window.ProcessName)))
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .Select(candidate => candidate.Window);

    private void AddCurrentWindowCommand(
        ICollection<(int Score, SearchResultViewModel Result)> candidates,
        string title,
        string subtitle,
        string glyph,
        Action action)
    {
        candidates.Add((
            250,
            new SearchResultViewModel(
                SearchResultKind.Action,
                title,
                subtitle,
                glyph,
                new RelayCommand(() =>
                {
                    action();
                    Query = string.Empty;
                }))));
    }

    private static bool TryGetArgument(string query, string command, out string argument)
    {
        var prefix = command + " ";
        if (query.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            argument = query[prefix.Length..].Trim();
            return true;
        }

        argument = string.Empty;
        return false;
    }

    private void AddActionCandidate(
        ICollection<(int Score, SearchResultViewModel Result)> candidates,
        string query,
        string title,
        string subtitle,
        string glyph,
        Func<bool> action)
    {
        var score = MatchScore(query, title, subtitle);
        if (score <= 0)
        {
            return;
        }

        candidates.Add((
            score + 10,
            new SearchResultViewModel(
                SearchResultKind.Action,
                title,
                subtitle,
                glyph,
                new RelayCommand(() =>
                {
                    _ = action();
                    Query = string.Empty;
                }))));
    }

    private void OnSettingsChanged(object? sender, SettingChangedEventArgs e)
    {
        if (HasQuery &&
            e.PropertyName is nameof(SettingsViewModel.SearchAppsEnabled)
                or nameof(SettingsViewModel.SearchWindowsEnabled)
                or nameof(SettingsViewModel.SearchActionsEnabled)
                or nameof(SettingsViewModel.SearchMacrosEnabled))
        {
            RefreshResults();
            NotifySearchState();
        }
    }

    private void NotifySearchState()
    {
        OnPropertyChanged(nameof(HasQuery));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasNoResults));
    }

    private static int MatchScore(string query, params string[] values)
    {
        var best = 0;

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (value.Equals(query, StringComparison.CurrentCultureIgnoreCase))
            {
                best = Math.Max(best, 100);
            }
            else if (value.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
            {
                best = Math.Max(best, 80);
            }
            else if (value.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            {
                best = Math.Max(best, 55);
            }
            else if (ContainsAllTokens(value, query))
            {
                best = Math.Max(best, 35);
            }
        }

        return best;
    }

    private static bool ContainsAllTokens(string value, string query)
    {
        var tokens = query.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return tokens.Length > 1 &&
               tokens.All(token => value.Contains(token, StringComparison.CurrentCultureIgnoreCase));
    }
}
