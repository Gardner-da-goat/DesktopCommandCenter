using System.Windows.Input;

namespace DesktopCommandCenter.App.ViewModels;

public enum SearchResultKind
{
    App,
    Window,
    Action
}

public sealed class SearchResultViewModel
{
    public SearchResultViewModel(
        SearchResultKind kind,
        string title,
        string subtitle,
        string glyph,
        ICommand executeCommand)
    {
        Kind = kind;
        Title = title;
        Subtitle = subtitle;
        Glyph = glyph;
        ExecuteCommand = executeCommand;
    }

    public SearchResultKind Kind { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string Glyph { get; }
    public ICommand ExecuteCommand { get; }
}
