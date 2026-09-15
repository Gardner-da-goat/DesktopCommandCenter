using DesktopCommandCenter.Core.Settings;

namespace DesktopCommandCenter.Core.State;

public sealed class SidebarState
{
    public const double CollapsedWidth = 34;

    public SidebarState(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        IsExpanded = !settings.StartCollapsed;
        ExpandedWidth = settings.SidebarWidth;
        AnimationsEnabled = settings.AnimationsEnabled;
    }

    public bool IsExpanded { get; private set; }
    public double ExpandedWidth { get; private set; }
    public bool AnimationsEnabled { get; private set; }
    public double CurrentWidth => IsExpanded ? ExpandedWidth : CollapsedWidth;

    public void Expand() => IsExpanded = true;
    public void Collapse() => IsExpanded = false;
    public void Toggle() => IsExpanded = !IsExpanded;
    public void SetWidth(double width) => ExpandedWidth = AppSettings.ClampSidebarWidth(width);
    public void SetAnimationsEnabled(bool enabled) => AnimationsEnabled = enabled;
}