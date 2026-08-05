namespace LangFilesEditor.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Models;

/// <summary>
/// Преобразует <see cref="RowVisualState"/> записи перевода в кисть фона строки грида.
/// </summary>
public class RowVisualStateToBrushConverter : IValueConverter
{
    private const string DefaultKey = "EditorRowDefaultBrush";
    private const string ErrorKey = "EditorRowErrorBrush";
    private const string WarningKey = "EditorRowWarningBrush";
    private const string UpdateKey = "EditorRowUpdateBrush";
    private const string MarkedKey = "EditorRowMarkedBrush";

    // Запасные значения светлой темы. Нужны там, где ресурсы приложения ещё
    // недоступны: конструктор окна до Application.Run и превью в дизайнере.
    private static readonly SolidColorBrush FallbackDefault = Brushes.White;
    private static readonly SolidColorBrush FallbackError = Freeze(0xFD, 0xEC, 0xEC);
    private static readonly SolidColorBrush FallbackWarning = Freeze(0xFF, 0xF4, 0xE5);
    private static readonly SolidColorBrush FallbackUpdate = Freeze(0xE8, 0xF6, 0xEA);
    private static readonly SolidColorBrush FallbackMarked = Freeze(0xF0, 0xE8, 0xF7);

    /// <summary>
    /// Возвращает кисть, соответствующую переданному <see cref="RowVisualState"/>.
    /// </summary>
    /// <param name="value">Значение <see cref="RowVisualState"/>.</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Не используется.</param>
    /// <param name="culture">Не используется.</param>
    /// <returns>Кисть фона строки.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is RowVisualState state ? ToBrush(state) : Resolve(DefaultKey, FallbackDefault);

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    /// <summary>
    /// Возвращает кисть для указанного визуального состояния строки; переиспользуется из code-behind
    /// (например, <see cref="UI.Windows.MainWindow.WorkSpace.TranslationEntryGridRow"/>) без обращения к WPF-конвертеру.
    /// Кисть берётся из ресурсов приложения, иначе строки грида остались бы светлыми в тёмной теме.
    /// </summary>
    /// <param name="state">Визуальное состояние строки.</param>
    /// <returns>Кисть фона строки.</returns>
    public static SolidColorBrush ToBrush(RowVisualState state) => state switch
    {
        RowVisualState.Error => Resolve(ErrorKey, FallbackError),
        RowVisualState.Warning => Resolve(WarningKey, FallbackWarning),
        RowVisualState.Update => Resolve(UpdateKey, FallbackUpdate),
        RowVisualState.Marked => Resolve(MarkedKey, FallbackMarked),
        _ => Resolve(DefaultKey, FallbackDefault),
    };

    private static SolidColorBrush Resolve(string key, SolidColorBrush fallback) =>
        Application.Current?.TryFindResource(key) as SolidColorBrush ?? fallback;

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
