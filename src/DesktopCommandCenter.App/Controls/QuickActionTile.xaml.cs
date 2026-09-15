using System.Windows;
using System.Windows.Input;

namespace DesktopCommandCenter.App.Controls;

public partial class QuickActionTile : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(string), typeof(QuickActionTile), new PropertyMetadata("•"));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(QuickActionTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsActionEnabledProperty = DependencyProperty.Register(
        nameof(IsActionEnabled), typeof(bool), typeof(QuickActionTile), new PropertyMetadata(true));

    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(QuickActionTile), new PropertyMetadata(null));

    public QuickActionTile() => InitializeComponent();

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsActionEnabled
    {
        get => (bool)GetValue(IsActionEnabledProperty);
        set => SetValue(IsActionEnabledProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
}
