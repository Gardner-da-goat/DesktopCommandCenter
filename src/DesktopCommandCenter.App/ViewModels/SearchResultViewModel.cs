using System.Windows.Input;

namespace DesktopCommandCenter.App.ViewModels;

public enum SearchResultKind
{
    App,
    Window,
    Action,
    Macro
}

public sealed class SearchResultViewModel
{
    public SearchResultViewModel(
        SearchResultKind kind,
        string title,
        string subtitle,
        string glyph,
        ICommand executeCommand,
        string? secondaryLabel = null,
        ICommand? secondaryCommand = null)
    {
        Kind = kind;
        Title = title;
        Subtitle = subtitle;
        Glyph = glyph;
        ExecuteCommand = executeCommand;
        SecondaryLabel = secondaryLabel;
        SecondaryCommand = secondaryCommand;
    }

    public SearchResultKind Kind { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Glyph { get; }
    public ICommand ExecuteCommand { get; }
    public string? SecondaryLabel { get; }
    public ICommand? SecondaryCommand { get; }
    public bool HasSecondaryAction => SecondaryCommand is not null;
}
