using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class RecentActivityViewModel : ObservableObject
{
    public ObservableCollection<RecentActivityItemViewModel> Items { get; } = [];
    public bool HasItems => Items.Count > 0;

    public void Add(string title, string subtitle, Action action)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var existing = Items.FirstOrDefault(item =>
            item.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase) &&
            item.Subtitle.Equals(subtitle, StringComparison.CurrentCultureIgnoreCase));

        if (existing is not null)
        {
            Items.Remove(existing);
        }

        RecentActivityItemViewModel? item = null;
        item = new RecentActivityItemViewModel(
            title,
            subtitle,
            DateTime.Now,
            new RelayCommand(() =>
            {
                action();
                if (item is not null)
                {
                    Items.Remove(item);
                    Items.Insert(0, item);
                }
            }));

        Items.Insert(0, item);

        while (Items.Count > 8)
        {
            Items.RemoveAt(Items.Count - 1);
        }

        OnPropertyChanged(nameof(HasItems));
    }
}

public sealed class RecentActivityItemViewModel(
    string title,
    string subtitle,
    DateTime occurredAt,
    ICommand runCommand)
{
    public string Title { get; } = title;
    public string Subtitle { get; } = subtitle;
    public string TimeText { get; } = occurredAt.ToString("t");
    public ICommand RunCommand { get; } = runCommand;
}
