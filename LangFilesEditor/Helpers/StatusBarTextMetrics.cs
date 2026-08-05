namespace LangFilesEditor.Helpers;

using System.Globalization;
using System.Windows;
using System.Windows.Media;

/// <summary>
/// Измерение ширины текстовых меток status bar для раскладки сегментов.
/// </summary>
internal static class StatusBarTextMetrics
{
    private const double FontSize = 11.5;
    private const double PixelsPerDip = 1.0;

    private static readonly Typeface Typeface = new(
        new FontFamily("Segoe UI"),
        FontStyles.Normal,
        FontWeights.Normal,
        FontStretches.Normal);

    /// <summary>
    /// Минимальная ширина метки иерархии, ниже которой текст считается нечитаемым и подлежит замене на минимальную ширину.
    /// </summary>
    public const double MinHierarchyLabelWidth = 40;

    /// <summary>
    /// Измеряет ширину текста метки status bar в пикселях при текущих настройках шрифта.
    /// </summary>
    /// <param name="text">Текст метки.</param>
    /// <returns>Ширина текста в пикселях, включая завершающие пробелы.</returns>
    public static double MeasureLabel(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface,
            FontSize,
            Brushes.Black,
            PixelsPerDip);

        return formatted.WidthIncludingTrailingWhitespace;
    }
}