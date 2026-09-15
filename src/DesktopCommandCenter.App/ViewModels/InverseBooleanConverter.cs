using System.Globalization;
using System.Windows.Data;

namespace DesktopCommandCenter.App.ViewModels;

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolean ? !boolean : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool boolean ? !boolean : value;
}