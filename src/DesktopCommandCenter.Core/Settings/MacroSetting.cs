namespace DesktopCommandCenter.Core.Settings;

public sealed class MacroSetting
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
}
