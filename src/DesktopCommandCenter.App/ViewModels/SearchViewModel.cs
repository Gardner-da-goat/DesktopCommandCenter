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
    private IReadOnlyList<InstalledAppInfo>? _installedApps;
    private string _query = string.Empty;

    public SearchViewModel(
        WindowsViewModel windowsViewModel,
        AppLauncherService appLauncher,
        ShellActionService shellActions,
        FavoritesViewModel favorites)
    {
        _windowsViewModel = windowsViewModel;
        _appLauncher = appLauncher;
        _shellActions = shellActions;
        _favorites = favorites;
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

        _windowsViewModel.Refresh();
        _installedApps ??= _appLauncher.GetInstalledApps();

        var candidates = new List<(int Score, SearchResultViewModel Result)>();

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

        foreach (var app in _installedApps)
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

        AddActionCandidate(candidates, query, "Downloads", "Open your Downloads folder", "⇩", _shellActions.OpenDownloads);
        AddActionCandidate(candidates, query, "Task Manager", "Open Task Manager", "▤", _shellActions.OpenTaskManager);
        AddActionCandidate(candidates, query, "Windows Settings", "Open Windows Settings", "⚙", _shellActions.OpenWindowsSettings);
        AddActionCandidate(candidates, query, "Terminal", "Open Windows Terminal", "⌨", _shellActions.OpenTerminal);

        foreach (var result in candidates
                     .OrderByDescending(candidate => candidate.Score)
                     .ThenBy(candidate => candidate.Result.Title, StringComparer.CurrentCultureIgnoreCase)
                     .Take(8)
                     .Select(candidate => candidate.Result))
        {
            Results.Add(result);
        }
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
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Length > 1 &&
               tokens.All(token => value.Contains(token, StringComparison.CurrentCultureIgnoreCase));
    }
}
