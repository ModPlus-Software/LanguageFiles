namespace LangFilesEditor.Converters;

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Models;

/// <summary>
/// Преобразует <see cref="RowVisualState"/> записи перевода в кисть фона строки грида.
/// </summary>
public class RowVisualStateToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Default = Brushes.White;
    private static readonly SolidColorBrush Error = Freeze(0xFF, 0xE0, 0xE0);
    private static readonly SolidColorBrush Warning = Freeze(0xFF, 0xF0, 0xD0);
    private static readonly SolidColorBrush Update = Freeze(0xDF, 0xF5, 0xE4);
    private static readonly SolidColorBrush Marked = Freeze(0xB3, 0xE5, 0xFC);

    /// <summary>
    /// Возвращает кисть, соответствующую переданному <see cref="RowVisualState"/>.
    /// </summary>
    /// <param name="value">Значение <see cref="RowVisualState"/>.</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Не используется.</param>
    /// <param name="culture">Не используется.</param>
    /// <returns>Кисть фона строки.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is RowVisualState state ? ToBrush(state) : Default;

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    /// <summary>
    /// Возвращает кисть для указанного визуального состояния строки; переиспользуется из code-behind
    /// (например, <see cref="UI.Windows.MainWindow.WorkSpace.TranslationEntryGridRow"/>) без обращения к WPF-конвертеру.
    /// </summary>
    /// <param name="state">Визуальное состояние строки.</param>
    /// <returns>Кисть фона строки.</returns>
    public static SolidColorBrush ToBrush(RowVisualState state) => state switch
    {
        RowVisualState.Error => Error,
        RowVisualState.Warning => Warning,
        RowVisualState.Update => Update,
        RowVisualState.Marked => Marked,
        _ => Default,
    };

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}