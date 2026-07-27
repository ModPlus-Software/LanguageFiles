namespace LangFilesEditor.Converters;

using System.Globalization;
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
    private static readonly SolidColorBrush Error = Freeze(0xC6, 0x28, 0x28);
    private static readonly SolidColorBrush Warning = Freeze(0xE0, 0x86, 0x00);
    private static readonly SolidColorBrush Update = Freeze(0x2E, 0x9E, 0x4F);

    /// <summary>
    /// Возвращает кисть, соответствующую переданной <see cref="DiagnosticSeverity"/>.
    /// </summary>
    /// <param name="value">Значение <see cref="DiagnosticSeverity"/>.</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Не используется.</param>
    /// <param name="culture">Не используется.</param>
    /// <returns>Кисть индикатора категории.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DiagnosticSeverity severity ? ToBrush(severity) : Error;

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static SolidColorBrush ToBrush(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => Error,
        DiagnosticSeverity.Warning => Warning,
        DiagnosticSeverity.Update => Update,
        _ => Error,
    };

    private static SolidColorBrush Freeze(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}