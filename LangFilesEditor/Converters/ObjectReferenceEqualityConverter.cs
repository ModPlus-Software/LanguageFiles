namespace LangFilesEditor.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>
/// Конвертер, возвращающий <see langword="true"/>, если два значения совпадают по ссылке.
/// </summary>
public class ObjectReferenceEqualityConverter : IMultiValueConverter
{
    /// <summary>
    /// Сравнивает первые два элемента массива значений через <see cref="object.ReferenceEquals"/>.
    /// </summary>
    /// <param name="values">Массив привязанных значений; используются элементы с индексами 0 и 1.</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Не используется.</param>
    /// <returns><see langword="true"/>, если оба значения ссылаются на один объект; иначе <see langword="false"/>.</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        // На этапе инициализации MultiBinding оба значения бывают UnsetValue/null — это не «совпадение».
        values.Length >= 2
        && values[0] != null
        && !ReferenceEquals(values[0], DependencyProperty.UnsetValue)
        && ReferenceEquals(values[0], values[1]);

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <param name="value">Значение для обратного преобразования.</param>
    /// <param name="targetTypes">Целевые типы привязки.</param>
    /// <param name="parameter">Дополнительный параметр.</param>
    /// <param name="culture">Не используется.</param>
    /// <returns>Всегда <see langword="null"/>.</returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
}