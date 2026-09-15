using System.Diagnostics;

namespace DesktopCommandCenter.Windows.Apps;

public sealed class AppLauncherService
{
    private static readonly string[] SupportedExtensions = [".lnk", ".appref-ms", ".url"];

    public IReadOnlyList<InstalledAppInfo> GetInstalledApps()
    {
        var byName = new Dictionary<string, InstalledAppInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in GetSearchRoots())
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                continue;
            }

            try
            {
                foreach (var path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    if (!SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var name = Path.GetFileNameWithoutExtension(path).Trim();
                    if (name.Length == 0)
                    {
                        continue;
                    }

                    byName.TryAdd(name, new InstalledAppInfo(name, path));
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Some shared Start Menu folders may be inaccessible. Other roots are still useful.
            }
            catch (IOException)
            {
                // A shortcut can disappear while enumerating. Ignore the affected root.
            }
        }

        return byName.Values
            .OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public bool Launch(InstalledAppInfo app)
    {
        ArgumentNullException.ThrowIfNull(app);

        try
        {
            Process.Start(new ProcessStartInfo(app.LaunchPath)
            {
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> GetSearchRoots()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
    }
}
