using System.Diagnostics;
using System.Text.Json;

namespace DesktopCommandCenter.App.Services;

public sealed record PulseDeckTrackSummary(
    string Title,
    string Artist,
    string Album,
    string FilePath,
    bool Favorite,
    int PlayCount,
    DateTime? LastPlayedAt);

public sealed record PulseDeckSnapshot(
    bool DataFound,
    string DataPath,
    int TrackCount,
    int PlaylistCount,
    int FavoriteCount,
    IReadOnlyList<PulseDeckTrackSummary> RecentTracks);

public sealed class PulseDeckIntegrationService
{
    public PulseDeckSnapshot ReadSnapshot()
    {
        var dataPath = FindDataPath();
        if (string.IsNullOrWhiteSpace(dataPath) || !File.Exists(dataPath))
        {
            return new PulseDeckSnapshot(
                false,
                string.Empty,
                0,
                0,
                0,
                []);
        }

        try
        {
            using var stream = File.OpenRead(dataPath);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            var tracks = new List<PulseDeckTrackSummary>();

            if (root.TryGetProperty("tracks", out var tracksElement) &&
                tracksElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var track in tracksElement.EnumerateArray())
                {
                    var title = ReadString(track, "title", "Unknown title");
                    var artist = ReadString(track, "artist", "Unknown artist");
                    var album = ReadString(track, "album", "Unknown album");
                    var filePath = ReadString(track, "filePath", string.Empty);
                    var favorite = ReadBool(track, "favorite");
                    var playCount = ReadInt(track, "playCount");
                    var lastPlayed = ReadDateTime(track, "lastPlayedAt");

                    tracks.Add(new PulseDeckTrackSummary(
                        title,
                        artist,
                        album,
                        filePath,
                        favorite,
                        playCount,
                        lastPlayed));
                }
            }

            var playlistCount =
                root.TryGetProperty("playlists", out var playlistsElement) &&
                playlistsElement.ValueKind == JsonValueKind.Array
                    ? playlistsElement.GetArrayLength()
                    : 0;

            var recent = tracks
                .OrderByDescending(track =>
                    track.LastPlayedAt ?? DateTime.MinValue)
                .ThenByDescending(track => track.PlayCount)
                .Take(8)
                .ToArray();

            return new PulseDeckSnapshot(
                true,
                dataPath,
                tracks.Count,
                playlistCount,
                tracks.Count(track => track.Favorite),
                recent);
        }
        catch
        {
            return new PulseDeckSnapshot(
                false,
                dataPath,
                0,
                0,
                0,
                []);
        }
    }

    public bool LaunchPulseDeck()
    {
        foreach (var candidate in CandidateLaunchPaths())
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                Process.Start(new ProcessStartInfo(candidate)
                {
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                // Try the next installation/shortcut location.
            }
        }

        return false;
    }

    public bool OpenPulseDeckDataFolder()
    {
        var snapshot = ReadSnapshot();
        var path = snapshot.DataFound
            ? Path.GetDirectoryName(snapshot.DataPath)
            : Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "PulseDeck");

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path)
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

    public bool IsPulseDeckInstalled() =>
        CandidateLaunchPaths().Any(File.Exists);

    private static string FindDataPath()
    {
        var roaming = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        var candidates = new[]
        {
            Path.Combine(roaming, "PulseDeck", "library.json"),
            Path.Combine(roaming, "PulseDeck-Store", "library.json")
        };

        return candidates.FirstOrDefault(File.Exists) ??
               candidates[0];
    }

    private static IEnumerable<string> CandidateLaunchPaths()
    {
        var local = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);
        var desktop = Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory);

        yield return Path.Combine(
            local,
            "Programs",
            "PulseDeck",
            "PulseDeck.exe");

        yield return Path.Combine(
            roaming,
            "Microsoft",
            "Windows",
            "Start Menu",
            "Programs",
            "PulseDeck.lnk");

        yield return Path.Combine(
            desktop,
            "PulseDeck.lnk");
    }

    private static string ReadString(
        JsonElement element,
        string propertyName,
        string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value;
    }

    private static bool ReadBool(
        JsonElement element,
        string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind is JsonValueKind.True;

    private static int ReadInt(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return 0;
        }

        return property.TryGetInt32(out var value)
            ? value
            : 0;
    }

    private static DateTime? ReadDateTime(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTime.TryParse(
            property.GetString(),
            out var value)
                ? value
                : null;
    }
}
