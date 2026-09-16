using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopCommandCenter.App.ViewModels;
using DesktopCommandCenter.Core.State;
using DesktopCommandCenter.Core.Settings;
using DesktopCommandCenter.Windows.Hotkeys;
using DesktopCommandCenter.Windows.Monitors;
using DesktopCommandCenter.Windows.Windows;
using Microsoft.Win32;

namespace DesktopCommandCenter.App.Views;

public partial class SidebarWindow : Window
{
    private readonly SidebarViewModel _viewModel;
    private readonly MonitorService _monitorService;
    private readonly WindowService _windowService;
    private readonly GlobalHotkeyService _hotkeyService = new();
    private HwndSource? _source;
    private nint _windowHandle;
    private bool _closingForExit;
    private IReadOnlyList<WindowReflowSnapshot> _reflowSnapshots = [];

    public SidebarWindow(
        SidebarViewModel viewModel,
        MonitorService monitorService,
        WindowService windowService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _monitorService = monitorService;
        _windowService = windowService;
        DataContext = viewModel;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        SizeChanged += (_, _) => AnchorToWorkingArea();
        Closing += OnClosing;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    public void CloseForExit()
    {
        RestoreAllWindows();
        _closingForExit = true;
        Close();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _windowHandle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WindowProc);
        ApplyHotkeyRegistration();
        ApplyHandlePosition();
    }

    private void ApplyHotkeyRegistration()
    {
        if (_windowHandle == 0)
        {
            return;
        }

        if (_viewModel.GlobalHotkeysEnabled)
        {
            _hotkeyService.RegisterDefaults(
                _windowHandle,
                _viewModel.ToggleHotkeyPreset,
                _viewModel.SearchHotkeyPreset,
                _viewModel.WindowControlHotkeysEnabled,
                _viewModel.SnapLeftHotkey,
                _viewModel.SnapRightHotkey,
                _viewModel.ToggleTopmostHotkey,
                _viewModel.OpacityUpHotkey,
                _viewModel.OpacityDownHotkey);
        }
        else
        {
            _hotkeyService.Unregister();
        }
    }

    private nint WindowProc(
        nint hwnd,
        int message,
        nint wParam,
        nint lParam,
        ref bool handled)
    {
        if (_hotkeyService.IsToggleSidebarMessage(message, wParam))
        {
            _viewModel.Toggle();
            handled = true;
        }
        else if (_hotkeyService.IsFocusSearchMessage(message, wParam))
        {
            _viewModel.ShowHome();
            _viewModel.Expand();
            Activate();
            Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                new Action(FocusSearchBox));
            handled = true;
        }
        else if (_hotkeyService.IsSnapLeftMessage(message, wParam))
        {
            ExecuteWindowHotkey(window => window.SnapLeftCommand.Execute(null));
            handled = true;
        }
        else if (_hotkeyService.IsSnapRightMessage(message, wParam))
        {
            ExecuteWindowHotkey(window => window.SnapRightCommand.Execute(null));
            handled = true;
        }
        else if (_hotkeyService.IsToggleTopmostMessage(message, wParam))
        {
            ExecuteWindowHotkey(window => window.IsAlwaysOnTop = !window.IsAlwaysOnTop);
            handled = true;
        }
        else if (_hotkeyService.IsOpacityUpMessage(message, wParam))
        {
            ExecuteWindowHotkey(window => window.Opacity = Math.Min(100, window.Opacity + 10));
            handled = true;
        }
        else if (_hotkeyService.IsOpacityDownMessage(message, wParam))
        {
            ExecuteWindowHotkey(window => window.Opacity = Math.Max(20, window.Opacity - 10));
            handled = true;
        }

        return 0;
    }

    private void ExecuteWindowHotkey(Action<WindowItemViewModel> action)
    {
        _viewModel.Windows.Refresh();
        var window = _viewModel.Windows.CurrentWindow ?? _viewModel.Windows.SelectedWindow;
        if (window is not null)
        {
            action(window);
        }
    }

    private void FocusSearchBox()
    {
        var searchBox = FindVisualChild<System.Windows.Controls.TextBox>(this);
        if (searchBox is null)
        {
            return;
        }

        searchBox.Focus();
        searchBox.SelectAll();
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private void OnCollapsedHandleMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _viewModel.Windows.CaptureForegroundNow();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Width = _viewModel.IsExpanded ? _viewModel.SidebarWidth : SidebarState.CollapsedWidth;
        AnchorToWorkingArea();

        if (_viewModel.IsExpanded)
        {
            ReflowAllWindows();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.IsExpanded))
        {
            if (_viewModel.IsExpanded)
            {
                ReflowAllWindows();
            }
            else
            {
                RestoreAllWindows();
                ExpandedPanel.Visibility = Visibility.Visible;
                CollapsedHandle.Visibility = Visibility.Collapsed;
            }

            AnimateWidth(_viewModel.IsExpanded ? _viewModel.SidebarWidth : SidebarState.CollapsedWidth);
        }
        else if (e.PropertyName == nameof(SidebarViewModel.SidebarWidth) && _viewModel.IsExpanded)
        {
            RestoreAllWindows();
            BeginAnimation(WidthProperty, null);
            Width = _viewModel.SidebarWidth;
            AnchorToWorkingArea();
            ReflowAllWindows();
        }
        else if (e.PropertyName == nameof(SidebarViewModel.GlobalHotkeysEnabled) ||
                 e.PropertyName == nameof(SidebarViewModel.WindowControlHotkeysEnabled) ||
                 e.PropertyName == nameof(SidebarViewModel.ToggleHotkeyPreset) ||
                 e.PropertyName == nameof(SidebarViewModel.SearchHotkeyPreset) ||
                 e.PropertyName == nameof(SidebarViewModel.SnapLeftHotkey) ||
                 e.PropertyName == nameof(SidebarViewModel.SnapRightHotkey) ||
                 e.PropertyName == nameof(SidebarViewModel.ToggleTopmostHotkey) ||
                 e.PropertyName == nameof(SidebarViewModel.OpacityUpHotkey) ||
                 e.PropertyName == nameof(SidebarViewModel.OpacityDownHotkey))
        {
            ApplyHotkeyRegistration();
        }
        else if (e.PropertyName == nameof(SidebarViewModel.SidebarEdge))
        {
            RestoreAllWindows();
            AnchorToWorkingArea();

            if (_viewModel.IsExpanded)
            {
                ReflowAllWindows();
            }
        }
        else if (e.PropertyName == nameof(SidebarViewModel.ReflowWindowsOnSidebar))
        {
            RestoreAllWindows();

            if (_viewModel.IsExpanded)
            {
                ReflowAllWindows();
            }
        }
        else if (e.PropertyName == nameof(SidebarViewModel.HandlePosition))
        {
            ApplyHandlePosition();
        }
    }

    private void ReflowAllWindows()
    {
        if (!_viewModel.ReflowWindowsOnSidebar || _reflowSnapshots.Count > 0)
        {
            return;
        }

        _reflowSnapshots = _windowService.ReflowAllForSidebar(
            _viewModel.SidebarWidth,
            _viewModel.IsSidebarOnLeft);
    }

    private void RestoreAllWindows()
    {
        if (_reflowSnapshots.Count == 0)
        {
            return;
        }

        _windowService.RestoreReflows(_reflowSnapshots);
        _reflowSnapshots = [];
    }

    private void ApplyHandlePosition()
    {
        CollapsedHandle.VerticalAlignment = _viewModel.HandlePosition switch
        {
            SidebarHandlePosition.Top => VerticalAlignment.Top,
            SidebarHandlePosition.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center
        };

        CollapsedHandle.Margin = _viewModel.HandlePosition switch
        {
            SidebarHandlePosition.Top => new Thickness(0, 18, 0, 0),
            SidebarHandlePosition.Bottom => new Thickness(0, 0, 0, 18),
            _ => new Thickness(0)
        };
    }

    private void AnimateWidth(double targetWidth)
    {
        BeginAnimation(WidthProperty, null);

        if (!_viewModel.AnimationsEnabled)
        {
            Width = targetWidth;
            AnchorToWorkingArea();
            CompleteVisualState();
            return;
        }

        var expanding = targetWidth > ActualWidth;
        var animation = new DoubleAnimation
        {
            From = ActualWidth,
            To = targetWidth,
            Duration = TimeSpan.FromMilliseconds(expanding ? 180 : 150),
            EasingFunction = expanding
                ? new CubicEase { EasingMode = EasingMode.EaseOut }
                : new QuadraticEase { EasingMode = EasingMode.EaseIn },
            FillBehavior = FillBehavior.Stop
        };

        animation.Completed += (_, _) =>
        {
            Width = targetWidth;
            AnchorToWorkingArea();
            CompleteVisualState();
        };

        BeginAnimation(
            WidthProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
    }

    private void CompleteVisualState()
    {
        ExpandedPanel.ClearValue(VisibilityProperty);
        CollapsedHandle.ClearValue(VisibilityProperty);
    }

    private void AnchorToWorkingArea()
    {
        if (!IsLoaded)
        {
            return;
        }

        var area = _monitorService.GetPrimaryWorkingArea();
        Height = area.Height;
        Top = area.Top;
        Left = _viewModel.IsSidebarOnLeft
            ? area.Left
            : area.Left + area.Width - ActualWidth;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            RestoreAllWindows();
            AnchorToWorkingArea();

            if (_viewModel.IsExpanded)
            {
                ReflowAllWindows();
            }
        });
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_closingForExit)
        {
            e.Cancel = true;
            _viewModel.Collapse();
            return;
        }

        RestoreAllWindows();
        _source?.RemoveHook(WindowProc);
        _source = null;
        _windowHandle = 0;
        _hotkeyService.Dispose();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }
}
