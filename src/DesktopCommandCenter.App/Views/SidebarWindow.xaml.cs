using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using DesktopCommandCenter.App.ViewModels;
using DesktopCommandCenter.Core.State;
using DesktopCommandCenter.Windows.Hotkeys;
using DesktopCommandCenter.Windows.Monitors;
using Microsoft.Win32;

namespace DesktopCommandCenter.App.Views;

public partial class SidebarWindow : Window
{
    private readonly SidebarViewModel _viewModel;
    private readonly MonitorService _monitorService;
    private readonly GlobalHotkeyService _hotkeyService = new();
    private HwndSource? _source;
    private bool _closingForExit;

    public SidebarWindow(SidebarViewModel viewModel, MonitorService monitorService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _monitorService = monitorService;
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
        _closingForExit = true;
        Close();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WindowProc);
        _ = _hotkeyService.RegisterToggleSidebar(handle);
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

        return 0;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Width = _viewModel.IsExpanded ? _viewModel.SidebarWidth : SidebarState.CollapsedWidth;
        AnchorToWorkingArea();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SidebarViewModel.IsExpanded))
        {
            if (!_viewModel.IsExpanded)
            {
                ExpandedPanel.Visibility = Visibility.Visible;
                CollapsedHandle.Visibility = Visibility.Collapsed;
            }

            AnimateWidth(_viewModel.IsExpanded ? _viewModel.SidebarWidth : SidebarState.CollapsedWidth);
        }
        else if (e.PropertyName == nameof(SidebarViewModel.SidebarWidth) && _viewModel.IsExpanded)
        {
            BeginAnimation(WidthProperty, null);
            Width = _viewModel.SidebarWidth;
            AnchorToWorkingArea();
        }
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
            Duration = TimeSpan.FromMilliseconds(expanding ? 220 : 185),
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
        BeginAnimation(WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private void CompleteVisualState()
    {
        ExpandedPanel.ClearValue(VisibilityProperty);
        CollapsedHandle.ClearValue(VisibilityProperty);
    }

    private void AnchorToWorkingArea()
    {
        if (!IsLoaded) return;
        var area = _monitorService.GetPrimaryWorkingArea();
        Height = area.Height;
        Top = area.Top;
        Left = area.Left + area.Width - ActualWidth;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.Invoke(AnchorToWorkingArea);

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_closingForExit)
        {
            e.Cancel = true;
            _viewModel.Collapse();
            return;
        }

        _source?.RemoveHook(WindowProc);
        _source = null;
        _hotkeyService.Dispose();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }
}
