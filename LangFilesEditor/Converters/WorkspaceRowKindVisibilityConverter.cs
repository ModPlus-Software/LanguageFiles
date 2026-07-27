namespace LangFilesEditor.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UI.Windows.MainWindow.WorkSpace;

/// <summary>
/// Конвертер видимости строки рабочей области: показывает элемент, если его <see cref="WorkspaceRowKind"/>
/// совпадает с ожидаемым значением, переданным в параметре.
/// </summary>
public class WorkspaceRowKindVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Преобразует тип строки рабочей области в <see cref="Visibility"/>.
    /// </summary>
    /// <param name="value">Текущий <see cref="WorkspaceRowKind"/> строки.</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Имя ожидаемого значения <see cref="WorkspaceRowKind"/> в виде строки.</param>
    /// <param name="culture">Не используется.</param>
    /// <returns><see cref="Visibility.Visible"/>, если тип совпадает; иначе <see cref="Visibility.Collapsed"/>.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string expectedName)
        {
            return Visibility.Collapsed;
        }
        
        if (!Enum.TryParse(expectedName, out WorkspaceRowKind expected))
        {
            return Visibility.Collapsed;
        }
        
        return value is WorkspaceRowKind kind && kind == expected
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
    
    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <param name="value">Значение для обратного преобразования.</param>
    /// <param name="targetType">Целевой тип привязки.</param>
    /// <param name="parameter">Дополнительный параметр.</param>
    /// <param name="culture">Не используется.</param>
    /// <returns>Не возвращает значение — выбрасывает исключение.</returns>
    /// <exception cref="NotSupportedException">Всегда, так как обратное преобразование не реализовано.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        => throw new NotSupportedException();
}