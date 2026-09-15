using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace DesktopCommandCenter.App.Services;

public sealed class UpdateService
{
    private static string BackupDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopCommandCenter",
        "Backup");

    private const string LatestReleaseApi =
        "https://api.github.com/repos/Gardner-da-goat/DesktopCommandCenter/releases/latest";

    private const string ReleaseAssetName = "DesktopCommandCenter-win-x64.zip";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public Version CurrentVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            return new Version(
                Math.Max(0, version.Major),
                Math.Max(0, version.Minor),
                Math.Max(0, version.Build));
        }
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(
        string? channel,
        CancellationToken cancellationToken = default)
    {
        var includePrerelease = string.Equals(
            channel,
            "Beta",
            StringComparison.OrdinalIgnoreCase);

        var endpoint = includePrerelease
            ? "https://api.github.com/repos/Gardner-da-goat/DesktopCommandCenter/releases?per_page=20"
            : LatestReleaseApi;

        using var response = await HttpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        if (!includePrerelease)
        {
            return ParseRelease(document.RootElement);
        }

        UpdateInfo? best = null;

        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draftElement) &&
                draftElement.GetBoolean())
            {
                continue;
            }

            var candidate = ParseRelease(release);
            if (candidate is null)
            {
                continue;
            }

            if (best is null || candidate.Version > best.Version)
            {
                best = candidate;
            }
        }

        return best;
    }

    private static UpdateInfo? ParseRelease(JsonElement root)
    {
        var tagName = root.TryGetProperty("tag_name", out var tagElement)
            ? tagElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        var versionText = tagName.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(versionText, out var version))
        {
            return null;
        }

        var downloadUrl = string.Empty;
        if (root.TryGetProperty("assets", out var assetsElement))
        {
            foreach (var asset in assetsElement.EnumerateArray())
            {
                var name = asset.TryGetProperty("name", out var nameElement)
                    ? nameElement.GetString()
                    : null;

                if (!string.Equals(
                        name,
                        ReleaseAssetName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                downloadUrl = asset.TryGetProperty(
                    "browser_download_url",
                    out var urlElement)
                    ? urlElement.GetString() ?? string.Empty
                    : string.Empty;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            return null;
        }

        var releasePage = root.TryGetProperty("html_url", out var htmlElement)
            ? htmlElement.GetString() ?? string.Empty
            : string.Empty;

        var notes = root.TryGetProperty("body", out var bodyElement)
            ? bodyElement.GetString() ?? string.Empty
            : string.Empty;

        return new UpdateInfo(
            version,
            versionText,
            downloadUrl,
            releasePage,
            notes);
    }

    public async Task<string> DownloadUpdateAsync(
        UpdateInfo update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        var updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopCommandCenter",
            "Updates");

        Directory.CreateDirectory(updateDirectory);
        var destinationPath = Path.Combine(
            updateDirectory,
            $"DesktopCommandCenter-{update.VersionText}.zip");

        using var response = await HttpClient.GetAsync(
            update.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(destinationPath);

        var buffer = new byte[81920];
        long downloaded = 0;

        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloaded += read;

            if (totalBytes is > 0)
            {
                progress?.Report(downloaded / (double)totalBytes.Value);
            }
        }

        progress?.Report(1);
        return destinationPath;
    }

    public bool LaunchUpdater(string zipPath)
    {
        if (!File.Exists(zipPath))
        {
            return false;
        }

        var targetDirectory = AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        if (!CanWriteDirectory(targetDirectory))
        {
            return false;
        }

        var executablePath = Path.Combine(targetDirectory, "DesktopCommandCenter.exe");
        if (!File.Exists(executablePath))
        {
            executablePath = Environment.ProcessPath ?? executablePath;
        }

        var updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopCommandCenter",
            "Updates");

        Directory.CreateDirectory(updateDirectory);
        var scriptPath = Path.Combine(updateDirectory, "apply-update.ps1");

        File.WriteAllText(scriptPath, """
            param(
                [int]$ProcessIdToWait,
                [string]$ZipPath,
                [string]$TargetDirectory,
                [string]$ExecutablePath,
                [string]$BackupDirectory
            )

            $ErrorActionPreference = 'Stop'

            try {
                Wait-Process -Id $ProcessIdToWait -ErrorAction SilentlyContinue
            } catch {
            }

            $staging = Join-Path ([System.IO.Path]::GetTempPath()) ("DesktopCommandCenter-Update-" + [Guid]::NewGuid().ToString("N"))

            try {
                New-Item -ItemType Directory -Path $staging -Force | Out-Null
                Expand-Archive -LiteralPath $ZipPath -DestinationPath $staging -Force

                Remove-Item -LiteralPath $BackupDirectory -Recurse -Force -ErrorAction SilentlyContinue
                New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
                Copy-Item -Path (Join-Path $TargetDirectory '*') -Destination $BackupDirectory -Recurse -Force

                Copy-Item -Path (Join-Path $staging '*') -Destination $TargetDirectory -Recurse -Force
                Start-Process -FilePath $ExecutablePath
            }
            finally {
                Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item -LiteralPath $ZipPath -Force -ErrorAction SilentlyContinue
            }
            """);

        try
        {
            var startInfo = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(scriptPath);
            startInfo.ArgumentList.Add("-ProcessIdToWait");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add("-ZipPath");
            startInfo.ArgumentList.Add(zipPath);
            startInfo.ArgumentList.Add("-TargetDirectory");
            startInfo.ArgumentList.Add(targetDirectory);
            startInfo.ArgumentList.Add("-ExecutablePath");
            startInfo.ArgumentList.Add(executablePath);
            startInfo.ArgumentList.Add("-BackupDirectory");
            startInfo.ArgumentList.Add(BackupDirectory);

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool HasRollbackBackup()
    {
        try
        {
            return Directory.Exists(BackupDirectory) &&
                   File.Exists(Path.Combine(BackupDirectory, "DesktopCommandCenter.exe"));
        }
        catch
        {
            return false;
        }
    }

    public bool LaunchRollback()
    {
        if (!HasRollbackBackup())
        {
            return false;
        }

        var targetDirectory = AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);

        if (!CanWriteDirectory(targetDirectory))
        {
            return false;
        }

        var executablePath = Path.Combine(
            targetDirectory,
            "DesktopCommandCenter.exe");

        var updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DesktopCommandCenter",
            "Updates");

        Directory.CreateDirectory(updateDirectory);
        var scriptPath = Path.Combine(updateDirectory, "rollback-update.ps1");

        File.WriteAllText(scriptPath, """
            param(
                [int]$ProcessIdToWait,
                [string]$BackupDirectory,
                [string]$TargetDirectory,
                [string]$ExecutablePath
            )

            $ErrorActionPreference = 'Stop'

            try {
                Wait-Process -Id $ProcessIdToWait -ErrorAction SilentlyContinue
            } catch {
            }

            Copy-Item -Path (Join-Path $BackupDirectory '*') -Destination $TargetDirectory -Recurse -Force
            Start-Process -FilePath $ExecutablePath
            """);

        try
        {
            var startInfo = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(scriptPath);
            startInfo.ArgumentList.Add("-ProcessIdToWait");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add("-BackupDirectory");
            startInfo.ArgumentList.Add(BackupDirectory);
            startInfo.ArgumentList.Add("-TargetDirectory");
            startInfo.ArgumentList.Add(targetDirectory);
            startInfo.ArgumentList.Add("-ExecutablePath");
            startInfo.ArgumentList.Add(executablePath);

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("DesktopCommandCenter", "0.2"));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static bool CanWriteDirectory(string directory)
    {
        try
        {
            var testPath = Path.Combine(directory, $".dcc-write-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(testPath, "test");
            File.Delete(testPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
