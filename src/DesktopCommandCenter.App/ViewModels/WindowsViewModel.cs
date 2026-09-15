using System.Collections.ObjectModel;
using System.Windows.Threading;
using DesktopCommandCenter.Windows.Windows;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class WindowsViewModel : ObservableObject, IDisposable
{
    private readonly WindowService _windowService;
    private readonly DispatcherTimer _refreshTimer;
    private WindowItemViewModel? _currentWindow;
    private WindowItemViewModel? _selectedWindow;
    private nint _lastExternalForegroundHandle;
    private string _statusMessage = "Ready";

    public WindowsViewModel(WindowService windowService)
    {
        _windowService = windowService;
        Windows = new ObservableCollection<WindowItemViewModel>();
        RefreshCommand = new RelayCommand(Refresh);

        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _refreshTimer.Tick += OnRefreshTimer;
        _refreshTimer.Start();

        Refresh();
    }

    public ObservableCollection<WindowItemViewModel> Windows { get; }
    public RelayCommand RefreshCommand { get; }

    public WindowItemViewModel? CurrentWindow
    {
        get => _currentWindow;
        private set
        {
            if (SetProperty(ref _currentWindow, value))
            {
                OnPropertyChanged(nameof(HasCurrentWindow));
            }
        }
    }

    public WindowItemViewModel? SelectedWindow
    {
        get => _selectedWindow;
        set
        {
            if (SetProperty(ref _selectedWindow, value))
            {
                OnPropertyChanged(nameof(HasSelectedWindow));
            }
        }
    }

    public bool HasCurrentWindow => CurrentWindow is not null;
    public bool HasSelectedWindow => SelectedWindow is not null;
    public int WindowCount => Windows.Count;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void Refresh()
    {
        try
        {
            var selectedHandle = SelectedWindow?.Handle ?? 0;
            var windowInfos = _windowService.GetWindows();
            var foreground = _windowService.GetForegroundWindowInfo();

            if (foreground is not null)
            {
                _lastExternalForegroundHandle = foreground.Handle;
            }

            var liveHandles = windowInfos
                .Select(info => info.Handle)
                .ToHashSet();

            if (_lastExternalForegroundHandle != 0 &&
                !liveHandles.Contains(_lastExternalForegroundHandle))
            {
                _lastExternalForegroundHandle = 0;
            }

            var activeHandle = _lastExternalForegroundHandle;
            var byHandle = Windows
                .GroupBy(item => item.Handle)
                .ToDictionary(group => group.Key, group => group.First());

            for (var index = Windows.Count - 1; index >= 0; index--)
            {
                if (!liveHandles.Contains(Windows[index].Handle))
                {
                    Windows.RemoveAt(index);
                }
            }

            foreach (var info in windowInfos)
            {
                if (!byHandle.TryGetValue(info.Handle, out var item) || !Windows.Contains(item))
                {
                    item = new WindowItemViewModel(info, _windowService);
                    Windows.Add(item);
                }

                item.Refresh(info, activeHandle);
            }

            CurrentWindow = Windows.FirstOrDefault(item => item.Handle == activeHandle);

            SelectedWindow =
                Windows.FirstOrDefault(item => item.Handle == selectedHandle) ??
                CurrentWindow ??
                Windows.FirstOrDefault();

            StatusMessage = WindowCount == 0
                ? "No normal application windows found."
                : $"{WindowCount} window{(WindowCount == 1 ? string.Empty : "s")} available.";

            OnPropertyChanged(nameof(WindowCount));
        }
        catch
        {
            StatusMessage = "Windows could not be refreshed. The sidebar is still usable.";
        }
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimer;
    }

    private void OnRefreshTimer(object? sender, EventArgs e) => Refresh();
}
