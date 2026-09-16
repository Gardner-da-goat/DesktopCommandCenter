using DesktopCommandCenter.App.Views;
using DesktopCommandCenter.Core.Settings;

namespace DesktopCommandCenter.App.Services;

public sealed class AmbienceService : IDisposable
{
    private AmbienceWindow? _window;

    public void Apply(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.AmbienceEnabled)
        {
            Stop();
            return;
        }

        if (_window is null)
        {
            _window = new AmbienceWindow(settings);
            _window.Closed += (_, _) => _window = null;
            _window.Show();
            return;
        }

        _window.ApplySettings(settings);
    }

    public void Stop()
    {
        if (_window is null)
        {
            return;
        }

        var window = _window;
        _window = null;
        window.CloseForService();
    }

    public void Dispose() => Stop();
}
