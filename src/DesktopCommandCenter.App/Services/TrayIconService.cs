using System.Drawing;
using System.Windows.Forms;

namespace DesktopCommandCenter.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIconService(
        Action openHub,
        Action openSidebar,
        Action collapseSidebar,
        Action settings,
        Action exit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Desktop Hub", null, (_, _) => openHub());
        menu.Items.Add("Open Quick Sidebar", null, (_, _) => openSidebar());
        menu.Items.Add("Collapse Sidebar", null, (_, _) => collapseSidebar());
        menu.Items.Add("Settings", null, (_, _) => settings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exit());

        _notifyIcon = new NotifyIcon
        {
            Text = "Desktop Command Center",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => openHub();
    }

    public void SetVisible(bool visible) => _notifyIcon.Visible = visible;

    public void ShowNotification(string title, string message)
    {
        if (!_notifyIcon.Visible ||
            string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(4000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
    }
}
