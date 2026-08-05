namespace LangFilesEditor.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Models;

/// <summary>
/// Преобразует <see cref="DiagnosticSeverity"/> в акцентную кисть индикатора категории диагностики
/// (см. <see cref="DiagnosticCategory.Severity"/>). Цвет — вопрос отображения, поэтому живёт здесь,
/// а не в сервисе диагностики или в самой модели категории.
/// </summary>
public class DiagnosticSeverityToBrushConverter : IValueConverter
{
    private const string ErrorKey = "EditorDiagnosticErrorBrush";
    private const string WarningKey = "EditorDiagnosticWarningBrush";
    private const string UpdateKey = "EditorDiagnosticUpdateBrush";

    // Запасные значения светлой темы — на случай, когда ресурсы приложения ещё недоступны
    // (создание окна до Application.Run, предпросмотр в дизайнере).
    private static readonly SolidColorBrush FallbackError = Freeze(0xE7, 0x45, 0x45);
    private static readonly SolidColorBrush FallbackWarning = Freeze(0xE8, 0x95, 0x2E);
    private static readonly SolidColorBrush FallbackUpdate = Freeze(0x4C, 0xAF, 0x50);

    /// <summary>
    /// Возвращает кисть, соответствующую переданной <see cref="DiagnosticSeverity"/>.
    /// </summary>
    /// <param name="value">Значение <see cref="DiagnosticSeverity"/>.</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Не используется.</param>
    /// <param name="culture">Не используется.</param>
    /// <returns>Кисть индикатора категории.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        // Отсутствие значения не должно выглядеть как ошибка — привязка просто не меняет кисть.
        value is DiagnosticSeverity severity ? ToBrush(severity) : Binding.DoNothing;

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    // Кисть берётся из ресурсов приложения: в тёмной теме акценты диагностики другие.
    private static SolidColorBrush ToBrush(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => Resolve(ErrorKey, FallbackError),
        DiagnosticSeverity.Warning => Resolve(WarningKey, FallbackWarning),
        DiagnosticSeverity.Update => Resolve(UpdateKey, FallbackUpdate),
        _ => Resolve(UpdateKey, FallbackUpdate),
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
