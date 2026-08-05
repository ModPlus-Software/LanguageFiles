namespace LangFilesEditor.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// MultiValueConverter для Visibility (Visible если совпадает выбранный Node)
/// </summary>
public class NodeVisibilityConverter : IMultiValueConverter
{
    /// <summary>
    /// Сравнивает выбранный и текущий узел по ссылке и возвращает соответствующую видимость.
    /// </summary>
    /// <param name="values">Массив из двух элементов: выбранный узел (0) и узел строки (1).</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Не используется.</param>
    /// <returns><see cref="System.Windows.Visibility.Visible"/>, если ссылки совпадают; иначе <see cref="System.Windows.Visibility.Collapsed"/>.</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // На этапе инициализации MultiBinding оба значения бывают UnsetValue/null — это не «совпадение».
        if (values.Length < 2 || values[0] == null || ReferenceEquals(values[0], DependencyProperty.UnsetValue))
        {
            return Visibility.Collapsed;
        }

        return ReferenceEquals(values[0], values[1])
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <param name="value">Значение для обратного преобразования.</param>
    /// <param name="targetTypes">Целевые типы привязки.</param>
    /// <param name="parameter">Дополнительный параметр.</param>
    /// <param name="culture">Не используется.</param>
    /// <returns>Всегда <see langword="null"/>.</returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}