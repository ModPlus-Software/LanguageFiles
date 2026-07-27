namespace LangFilesEditor.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// Для <see cref="FrameworkElement.ToolTip"/>: пустая/пробельная строка → нет подсказки;
/// иначе возвращает исходный текст. Иначе WPF показывает пустой всплывающий tooltip.
/// </summary>
public sealed class NullIfEmptyStringConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text && !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        
        return DependencyProperty.UnsetValue;
    }
    
    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}