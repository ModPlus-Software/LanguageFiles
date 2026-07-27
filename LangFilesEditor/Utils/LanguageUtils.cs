namespace LangFilesEditor.Utils;

using System.Globalization;

// todo: мб это куда-то перенести или переименовать как-то?
/// <summary>
/// Утилиты форматирования кодов языков для отображения в интерфейсе.
/// </summary>
public static class LanguageUtils
{
    /// <summary>
    /// Формирует строку с локализованными названиями языков, разделёнными запятыми.
    /// </summary>
    /// <param name="languages">Список кодов культур (например, "ru-RU", "en-US").</param>
    /// <returns>Строка вида "русский (Россия)", "английский (США)", etc.</returns>
    public static string FormatDisplayOrder(IReadOnlyList<string> languages) =>
        string.Join(", ", languages.Select(l => CultureInfo.GetCultureInfo(l).DisplayName));
}