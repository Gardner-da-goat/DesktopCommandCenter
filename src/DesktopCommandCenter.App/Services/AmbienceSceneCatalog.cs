using System.Windows.Media;

namespace DesktopCommandCenter.App.Services;

public sealed record AmbienceScenePreset(
    string Name,
    Color TopColor,
    Color BottomColor,
    Color AccentColor,
    Color SecondaryAccent,
    bool OceanScene,
    bool NightScene);

public static class AmbienceSceneCatalog
{
    public const string CoralReef = "Coral Reef";
    public const string DeepOcean = "Deep Ocean";
    public const string KelpForest = "Kelp Forest";
    public const string FirefliesNight = "Fireflies Night";
    public const string MinimalGradient = "Minimal Gradient";
    public const string Space = "Space";

    private static readonly IReadOnlyDictionary<string, AmbienceScenePreset> Presets =
        new Dictionary<string, AmbienceScenePreset>(StringComparer.OrdinalIgnoreCase)
        {
            [CoralReef] = new(
                CoralReef,
                Color.FromRgb(22, 122, 164),
                Color.FromRgb(4, 45, 76),
                Color.FromRgb(255, 118, 103),
                Color.FromRgb(255, 190, 84),
                OceanScene: true,
                NightScene: false),
            [DeepOcean] = new(
                DeepOcean,
                Color.FromRgb(4, 53, 88),
                Color.FromRgb(1, 15, 36),
                Color.FromRgb(75, 188, 224),
                Color.FromRgb(53, 111, 170),
                OceanScene: true,
                NightScene: false),
            [KelpForest] = new(
                KelpForest,
                Color.FromRgb(14, 100, 102),
                Color.FromRgb(2, 38, 48),
                Color.FromRgb(69, 158, 107),
                Color.FromRgb(121, 187, 104),
                OceanScene: true,
                NightScene: false),
            [FirefliesNight] = new(
                FirefliesNight,
                Color.FromRgb(21, 33, 58),
                Color.FromRgb(5, 17, 28),
                Color.FromRgb(255, 225, 106),
                Color.FromRgb(47, 104, 76),
                OceanScene: false,
                NightScene: true),
            [MinimalGradient] = new(
                MinimalGradient,
                Color.FromRgb(32, 43, 66),
                Color.FromRgb(12, 18, 30),
                Color.FromRgb(80, 135, 255),
                Color.FromRgb(91, 207, 197),
                OceanScene: false,
                NightScene: false),
            [Space] = new(
                Space,
                Color.FromRgb(20, 16, 48),
                Color.FromRgb(3, 5, 17),
                Color.FromRgb(127, 106, 255),
                Color.FromRgb(44, 187, 218),
                OceanScene: false,
                NightScene: true)
        };

    public static AmbienceScenePreset Get(string? name)
    {
        var normalized = NormalizeName(name);
        return Presets[normalized];
    }

    public static string NormalizeName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && Presets.ContainsKey(name)
            ? Presets[name].Name
            : CoralReef;

    public static IReadOnlyList<string> Names { get; } =
    [
        CoralReef,
        DeepOcean,
        KelpForest,
        FirefliesNight,
        MinimalGradient,
        Space
    ];
}
