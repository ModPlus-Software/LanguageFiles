using LangFilesEditor.UI.Models;

namespace LangFilesEditor.Helpers;

/// <summary>
/// Отображаемые названия языков для UI.
/// </summary>
public static class LanguageDisplayHelper
{
    private static readonly Dictionary<string, string> KnownTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ru-RU"] = "Русский",
        ["uk-UA"] = "Українська",
        ["en-US"] = "English",
        ["de-DE"] = "Deutsch",
        ["es-ES"] = "Español",
        ["zh-CN"] = "中文",
    };

    /// <summary>
    /// Строит read-only список языков для настроек в порядке колонок грида.
    /// </summary>
    /// <param name="languageCodes">Коды языков из репозитория.</param>
    public static IReadOnlyList<LanguageDisplayInfo> BuildDisplayList(IReadOnlyList<string> languageCodes)
    {
        if (languageCodes == null || languageCodes.Count == 0)
        {
            return [];
        }

        return languageCodes
            .Select(code => new LanguageDisplayInfo
            {
                Code = code,
                Title = KnownTitles.GetValueOrDefault(code, code),
            })
            .ToList();
    }
}