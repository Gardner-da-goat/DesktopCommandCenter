using System.Windows;
using DesktopCommandCenter.Windows.Shell;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class ClipboardViewModel : ObservableObject
{
    private readonly ShellActionService _shellActions;
    private string _preview = "Clipboard is empty.";
    private string _status = "Ready";
    private bool _hasText;

    public ClipboardViewModel(ShellActionService shellActions)
    {
        _shellActions = shellActions;
        RefreshCommand = new RelayCommand(Refresh);
        ClearCommand = new RelayCommand(Clear);
        OpenHistoryCommand = new RelayCommand(() =>
        {
            _ = _shellActions.OpenClipboardHistory();
            Status = "Opened Windows clipboard history.";
        });

        Refresh();
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand OpenHistoryCommand { get; }

    public string Preview
    {
        get => _preview;
        private set => SetProperty(ref _preview, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool HasText
    {
        get => _hasText;
        private set => SetProperty(ref _hasText, value);
    }

    private void Refresh()
    {
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                var text = System.Windows.Clipboard.GetText().Replace("\r", " ").Replace("\n", " ").Trim();
                HasText = text.Length > 0;
                Preview = text.Length > 180
                    ? text[..180] + "…"
                    : text;
                Status = HasText ? "Text clipboard ready." : "Clipboard is empty.";
                return;
            }

            if (System.Windows.Clipboard.ContainsFileDropList())
            {
                var files = System.Windows.Clipboard.GetFileDropList();
                HasText = false;
                Preview = files.Count == 1
                    ? files[0] ?? "File on clipboard"
                    : $"{files.Count} files on clipboard";
                Status = "File clipboard detected.";
                return;
            }

            HasText = false;
            Preview = "Clipboard is empty.";
            Status = "Ready";
        }
        catch
        {
            HasText = false;
            Preview = "Clipboard is temporarily busy.";
            Status = "Try Refresh again.";
        }
    }

    private void Clear()
    {
        try
        {
            System.Windows.Clipboard.Clear();
            HasText = false;
            Preview = "Clipboard is empty.";
            Status = "Clipboard cleared.";
        }
        catch
        {
            Status = "Clipboard is busy. Try again.";
        }
    }
}
