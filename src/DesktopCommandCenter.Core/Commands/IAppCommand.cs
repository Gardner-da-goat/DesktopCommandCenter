namespace DesktopCommandCenter.Core.Commands;

public interface IAppCommand
{
    string Id { get; }
    string DisplayName { get; }
    bool CanExecute { get; }
}