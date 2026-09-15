using System.Drawing;
using System.Windows.Forms;

namespace DesktopCommandCenter.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public TrayIconService(Action open, Action collapse, Action settings, Action exit)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => open());
        menu.Items.Add("Collapse", null, (_, _) => collapse());
        menu.Items.Add("Settings", null, (_, _) => settings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exit());

        _notifyIcon = new NotifyIcon
        {
            Text = "Desktop Command Center",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => open();
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