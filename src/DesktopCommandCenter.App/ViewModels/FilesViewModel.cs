using System.Collections.ObjectModel;
using System.IO;
using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed record FileExplorerItem(
    string Name,
    string Path,
    bool IsDirectory,
    string Type,
    string SizeText,
    DateTime? Modified);

public sealed class FilesViewModel : ObservableObject
{
    private readonly ShellActionService _shell;
    private readonly Stack<string> _backStack = new();
    private string _currentPath;
    private FileExplorerItem? _selectedItem;
    private string _statusText = string.Empty;

    public FilesViewModel(ShellActionService shell)
    {
        _shell = shell;
        _currentPath = DefaultRoot();

        Items = [];
        QuickLocations = BuildQuickLocations();

        RefreshCommand = new RelayCommand(Refresh);
        UpCommand = new RelayCommand(GoUp, CanGoUp);
        BackCommand = new RelayCommand(GoBack, () => _backStack.Count > 0);
        OpenSelectedCommand = new RelayCommand(OpenSelected, () => SelectedItem is not null);
        OpenInExplorerCommand = new RelayCommand(
            () => _ = _shell.OpenPath(CurrentPath));

        Refresh();
    }

    public ObservableCollection<FileExplorerItem> Items { get; }
    public IReadOnlyList<FileExplorerItem> QuickLocations { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand UpCommand { get; }
    public RelayCommand BackCommand { get; }
    public RelayCommand OpenSelectedCommand { get; }
    public RelayCommand OpenInExplorerCommand { get; }

    public string CurrentPath
    {
        get => _currentPath;
        private set
        {
            if (SetProperty(ref _currentPath, value))
            {
                UpCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public FileExplorerItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                OpenSelectedCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public void Refresh()
    {
        Items.Clear();

        try
        {
            if (!Directory.Exists(CurrentPath))
            {
                StatusText = "This folder is unavailable.";
                return;
            }

            var directory = new DirectoryInfo(CurrentPath);

            foreach (var folder in directory
                         .EnumerateDirectories()
                         .Where(item => !item.Attributes.HasFlag(FileAttributes.Hidden))
                         .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                         .Take(300))
            {
                Items.Add(new FileExplorerItem(
                    folder.Name,
                    folder.FullName,
                    true,
                    "Folder",
                    string.Empty,
                    SafeModified(folder)));
            }

            foreach (var file in directory
                         .EnumerateFiles()
                         .Where(item => !item.Attributes.HasFlag(FileAttributes.Hidden))
                         .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                         .Take(Math.Max(0, 500 - Items.Count)))
            {
                Items.Add(new FileExplorerItem(
                    file.Name,
                    file.FullName,
                    false,
                    string.IsNullOrWhiteSpace(file.Extension)
                        ? "File"
                        : file.Extension.TrimStart('.').ToUpperInvariant(),
                    FormatSize(file.Length),
                    SafeModified(file)));
            }

            StatusText = $"{Items.Count} items";
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "Access denied to this folder.";
        }
        catch (IOException)
        {
            StatusText = "Windows could not read this folder.";
        }
        catch
        {
            StatusText = "This folder could not be loaded.";
        }
    }

    public void OpenItem(FileExplorerItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (!item.IsDirectory)
        {
            _ = _shell.OpenPath(item.Path);
            return;
        }

        NavigateTo(item.Path);
    }

    public void NavigateTo(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !Directory.Exists(path) ||
            path.Equals(CurrentPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _backStack.Push(CurrentPath);
        CurrentPath = path;
        SelectedItem = null;
        BackCommand.RaiseCanExecuteChanged();
        Refresh();
    }

    public void NavigateQuick(FileExplorerItem? item) =>
        OpenItem(item);

    private void OpenSelected() => OpenItem(SelectedItem);

    private void GoBack()
    {
        if (_backStack.Count == 0)
        {
            return;
        }

        CurrentPath = _backStack.Pop();
        SelectedItem = null;
        BackCommand.RaiseCanExecuteChanged();
        Refresh();
    }

    private void GoUp()
    {
        try
        {
            var parent = Directory.GetParent(CurrentPath);
            if (parent is not null)
            {
                NavigateTo(parent.FullName);
            }
        }
        catch
        {
            // Navigation is best-effort.
        }
    }

    private bool CanGoUp()
    {
        try
        {
            return Directory.GetParent(CurrentPath) is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string DefaultRoot()
    {
        var profile = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);

        if (!string.IsNullOrWhiteSpace(profile) && Directory.Exists(profile))
        {
            return profile;
        }

        return Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory);
    }

    private static IReadOnlyList<FileExplorerItem> BuildQuickLocations()
    {
        var locations = new List<FileExplorerItem>();

        AddLocation(
            locations,
            "Home",
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        AddLocation(
            locations,
            "Desktop",
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        AddLocation(
            locations,
            "Documents",
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        AddLocation(
            locations,
            "Pictures",
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        AddLocation(
            locations,
            "Music",
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));

        var profile = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile))
        {
            AddLocation(
                locations,
                "Downloads",
                System.IO.Path.Combine(profile, "Downloads"));
        }

        return locations;
    }

    private static void AddLocation(
        ICollection<FileExplorerItem> locations,
        string name,
        string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return;
        }

        locations.Add(new FileExplorerItem(
            name,
            path,
            true,
            "Folder",
            string.Empty,
            null));
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = Math.Max(0, bytes);
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{size:0} {units[unit]}"
            : $"{size:0.#} {units[unit]}";
    }

    private static DateTime? SafeModified(FileSystemInfo item)
    {
        try
        {
            return item.LastWriteTime;
        }
        catch
        {
            return null;
        }
    }
}
