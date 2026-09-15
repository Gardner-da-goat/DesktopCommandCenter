namespace DesktopCommandCenter.Windows.Files;

public sealed record FileSearchResult(
    string Name,
    string Path,
    bool IsDirectory,
    int Score);

public sealed class FileSearchService
{
    private readonly object _gate = new();
    private IReadOnlyList<IndexedPath> _items = [];
    private int _started;

    public void StartIndexing()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        _ = Task.Run(BuildIndex);
    }

    public IReadOnlyList<FileSearchResult> Search(string query, int maxResults = 6)
    {
        if (string.IsNullOrWhiteSpace(query) || maxResults <= 0)
        {
            return [];
        }

        var trimmed = query.Trim();
        IReadOnlyList<IndexedPath> snapshot;

        lock (_gate)
        {
            snapshot = _items;
        }

        return snapshot
            .Select(item => new
            {
                Item = item,
                Score = MatchScore(trimmed, item.Name, item.Path)
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(maxResults)
            .Select(candidate => new FileSearchResult(
                candidate.Item.Name,
                candidate.Item.Path,
                candidate.Item.IsDirectory,
                candidate.Score))
            .ToArray();
    }

    private void BuildIndex()
    {
        var items = new List<IndexedPath>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in GetRoots())
        {
            if (!Directory.Exists(root) || !seen.Add(root))
            {
                continue;
            }

            items.Add(new IndexedPath(
                new DirectoryInfo(root).Name,
                root,
                true));

            try
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    MaxRecursionDepth = 3,
                    AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
                };

                foreach (var entry in Directory
                             .EnumerateFileSystemEntries(root, "*", options)
                             .Take(3000))
                {
                    if (!seen.Add(entry))
                    {
                        continue;
                    }

                    bool isDirectory;
                    try
                    {
                        isDirectory = Directory.Exists(entry);
                    }
                    catch
                    {
                        continue;
                    }

                    var name = System.IO.Path.GetFileName(entry);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    items.Add(new IndexedPath(name, entry, isDirectory));
                }
            }
            catch
            {
                // File search is best-effort. One inaccessible folder must not affect the app.
            }
        }

        lock (_gate)
        {
            _items = items;
        }
    }

    private static IEnumerable<string> GetRoots()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var downloads = string.IsNullOrWhiteSpace(profile)
            ? string.Empty
            : System.IO.Path.Combine(profile, "Downloads");

        foreach (var path in new[] { desktop, documents, downloads })
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                yield return path;
            }
        }
    }

    private static int MatchScore(string query, string name, string path)
    {
        if (name.Equals(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 100;
        }

        if (name.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 80;
        }

        if (name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 60;
        }

        if (path.Contains(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 35;
        }

        var tokens = query.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length > 1 &&
            tokens.All(token =>
                name.Contains(token, StringComparison.CurrentCultureIgnoreCase) ||
                path.Contains(token, StringComparison.CurrentCultureIgnoreCase)))
        {
            return 30;
        }

        return 0;
    }

    private sealed record IndexedPath(
        string Name,
        string Path,
        bool IsDirectory);
}
