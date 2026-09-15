using System.Windows.Input;
using DesktopCommandCenter.Windows.Windows;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class WindowItemViewModel : ObservableObject
{
    private readonly WindowService _windowService;
    private WindowInfo _window;
    private bool _isActive;
    private bool _isAlwaysOnTop;
    private int _opacity;

    public WindowItemViewModel(WindowInfo window, WindowService windowService)
    {
        _window = window;
        _windowService = windowService;
        _opacity = _windowService.GetOpacity(window.Handle);
        _isAlwaysOnTop = _windowService.IsAlwaysOnTop(window.Handle);

        ActivateCommand = new RelayCommand(() => _windowService.Activate(Handle));
        MinimizeCommand = new RelayCommand(() => _windowService.Minimize(Handle));
        MaximizeCommand = new RelayCommand(() => _windowService.Maximize(Handle));
        RestoreCommand = new RelayCommand(() => _windowService.Restore(Handle));
        CloseCommand = new RelayCommand(() => _windowService.Close(Handle));
        SnapLeftCommand = new RelayCommand(() => _windowService.Snap(Handle, WindowSnapPosition.Left));
        SnapRightCommand = new RelayCommand(() => _windowService.Snap(Handle, WindowSnapPosition.Right));
        CenterCommand = new RelayCommand(() => _windowService.Center(Handle));
        MoveToNextMonitorCommand = new RelayCommand(() => _windowService.MoveToNextMonitor(Handle));
    }

    public nint Handle => _window.Handle;
    public string Title => _window.Title;
    public string ProcessName => _window.ProcessName;
    public uint ProcessId => _window.ProcessId;

    public bool IsActive
    {
        get => _isActive;
        private set => SetProperty(ref _isActive, value);
    }

    public int Opacity
    {
        get => _opacity;
        set
        {
            var clamped = Math.Clamp(value, 20, 100);
            if (_opacity == clamped)
            {
                return;
            }

            if (_windowService.SetOpacity(Handle, clamped))
            {
                SetProperty(ref _opacity, clamped);
            }
        }
    }

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set
        {
            if (_isAlwaysOnTop == value)
            {
                return;
            }

            if (_windowService.SetAlwaysOnTop(Handle, value))
            {
                SetProperty(ref _isAlwaysOnTop, value);
            }
        }
    }

    public ICommand ActivateCommand { get; }
    public ICommand MinimizeCommand { get; }
    public ICommand MaximizeCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand SnapLeftCommand { get; }
    public ICommand SnapRightCommand { get; }
    public ICommand CenterCommand { get; }
    public ICommand MoveToNextMonitorCommand { get; }

    public void Refresh(WindowInfo window, nint activeHandle)
    {
        var titleChanged = !string.Equals(_window.Title, window.Title, StringComparison.Ordinal);
        var processChanged = !string.Equals(_window.ProcessName, window.ProcessName, StringComparison.Ordinal);
        _window = window;

        if (titleChanged)
        {
            OnPropertyChanged(nameof(Title));
        }

        if (processChanged)
        {
            OnPropertyChanged(nameof(ProcessName));
        }

        IsActive = Handle == activeHandle;

        var opacity = _windowService.GetOpacity(Handle);
        if (_opacity != opacity)
        {
            _opacity = opacity;
            OnPropertyChanged(nameof(Opacity));
        }

        var topmost = _windowService.IsAlwaysOnTop(Handle);
        if (_isAlwaysOnTop != topmost)
        {
            _isAlwaysOnTop = topmost;
            OnPropertyChanged(nameof(IsAlwaysOnTop));
        }
    }
}
