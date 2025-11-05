namespace LangFilesEditor;

using System.Globalization;
using System.Windows.Data;

/// <summary>
/// Возвращает true, если SelectedNode и текущий Node совпадают по ссылке.
/// </summary>
public class ObjectReferenceEqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return false;
        return ReferenceEquals(values[0], values[1]);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => null; // не нужен
}

/// <summary>
/// MultiValueConverter для Visibility (Visible если совпадает выбранный Node)
/// </summary>
public class NodeVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return System.Windows.Visibility.Collapsed;
        return ReferenceEquals(values[0], values[1])
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => null;
}