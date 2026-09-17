namespace DesktopCommandCenter.App.ViewModels;

public sealed record SettingsCategory(
    string Name,
    string Description,
    string Icon = "⚙",
    string Keywords = "");
