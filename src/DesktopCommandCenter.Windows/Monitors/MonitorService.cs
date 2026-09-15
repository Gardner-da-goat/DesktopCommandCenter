using System.Runtime.InteropServices;
using DesktopCommandCenter.Windows.Interop;

namespace DesktopCommandCenter.Windows.Monitors;

public sealed class MonitorService
{
    public WorkArea GetPrimaryWorkingArea()
    {
        var monitor = NativeMethods.MonitorFromPoint(new NativeMethods.Point(0, 0), NativeMethods.MonitorDefaultToPrimary);
        var info = new NativeMethods.MonitorInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>()
        };

        if (monitor == 0 || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return new WorkArea(0, 0, 1920, 1080);
        }

        var dpi = Math.Max(96u, NativeMethods.GetDpiForSystem());
        var scale = dpi / 96d;
        return new WorkArea(
            info.Work.Left / scale,
            info.Work.Top / scale,
            (info.Work.Right - info.Work.Left) / scale,
            (info.Work.Bottom - info.Work.Top) / scale);
    }
}