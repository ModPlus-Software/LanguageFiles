namespace LangFilesEditor.Services.RepositoryServices;

using System.IO;

/// <summary>
/// Построение путей к XML-файлам локализации в каталоге LanguageFiles.
/// <para>
/// В каталоге языка соседствуют два соглашения об именовании: общий файл домена
/// (<c>Revit.xml</c>, <c>Revit_Architecture.xml</c>) и вынесенный файл отдельного плагина
/// с суффиксом языка (<c>Revit_mprAlignViews_ru.xml</c>, <c>Revit_mprAlignViews_de.xml</c>).
/// Наружу оба варианта представлены одним языконезависимым ключом источника
/// (<c>Revit_mprAlignViews</c>), а сопоставление ключа с реальным файлом конкретного языка
/// выполняется здесь.
/// </para>
/// </summary>
internal static class LanguageFilePaths
{
    /// <summary>
    /// Возвращает каталог одного языка внутри корня LanguageFiles.
    /// </summary>
    /// <param name="languageFilesRoot">Корневой каталог LanguageFiles.</param>
    /// <param name="languageName">Код языка (имя подпапки).</param>
    public static string GetLanguageDirectory(string languageFilesRoot, string languageName) =>
        Path.Combine(languageFilesRoot, languageName);

    /// <summary>
    /// Возвращает суффикс имени файла для языка: <c>ru-RU</c> → <c>ru</c>.
    /// </summary>
    /// <param name="languageName">Код языка (имя подпапки).</param>
    /// <returns>Короткий код языка или пустая строка, если код не задан.</returns>
    public static string GetLanguageSuffix(string languageName)
    {
        if (string.IsNullOrEmpty(languageName))
        {
            return string.Empty;
        }

        var separatorIndex = languageName.IndexOf('-');
        return separatorIndex > 0 ? languageName[..separatorIndex] : languageName;
    }

    /// <summary>
    /// Приводит имя файла конкретного языка к языконезависимому ключу источника:
    /// <c>Revit_mprAlignViews_ru</c> → <c>Revit_mprAlignViews</c>. Имена без суффикса языка
    /// (<c>Revit</c>, <c>Revit_Architecture</c>) возвращаются без изменений.
    /// </summary>
    /// <param name="fileNameWithoutExtension">Имя файла без расширения.</param>
    /// <param name="languageName">Код языка каталога, из которого взят файл.</param>
    public static string GetSourceKey(string fileNameWithoutExtension, string languageName)
    {
        if (string.IsNullOrEmpty(fileNameWithoutExtension))
        {
            return fileNameWithoutExtension;
        }

        var languageSuffix = GetLanguageSuffix(languageName);
        if (languageSuffix.Length == 0)
        {
            return fileNameWithoutExtension;
        }

        var suffix = $"_{languageSuffix}";
        return fileNameWithoutExtension.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? fileNameWithoutExtension[..^suffix.Length]
            : fileNameWithoutExtension;
    }

    /// <summary>
    /// Возвращает путь к XML-файлу источника для языка. Сначала проверяется общий файл
    /// (<c>{ключ}.xml</c>), затем файл отдельного плагина (<c>{ключ}_{язык}.xml</c>).
    /// Если не существует ни одного, возвращается путь к общему файлу — вызывающий код
    /// сам решает, что делать с отсутствующим файлом.
    /// </summary>
    /// <param name="languageFilesRoot">Корневой каталог LanguageFiles.</param>
    /// <param name="languageName">Код языка.</param>
    /// <param name="sourceKey">Языконезависимый ключ источника (имя файла без расширения и суффикса языка).</param>
    public static string GetSourceFilePath(string languageFilesRoot, string languageName, string sourceKey)
    {
        var languageDirectory = GetLanguageDirectory(languageFilesRoot, languageName);
        var sharedFilePath = Path.Combine(languageDirectory, $"{sourceKey}.xml");
        if (File.Exists(sharedFilePath))
        {
            return sharedFilePath;
        }

        var languageSuffix = GetLanguageSuffix(languageName);
        if (languageSuffix.Length == 0)
        {
            return sharedFilePath;
        }

        var pluginFilePath = Path.Combine(languageDirectory, $"{sourceKey}_{languageSuffix}.xml");
        return File.Exists(pluginFilePath) ? pluginFilePath : sharedFilePath;
    }

    /// <summary>
    /// Перечисляет существующие пути к файлу источника во всех языковых подпапках
    /// с учётом обоих соглашений об именовании.
    /// </summary>
    /// <param name="languageFilesRoot">Корневой каталог LanguageFiles.</param>
    /// <param name="sourceKey">Языконезависимый ключ источника.</param>
    public static IEnumerable<string> EnumerateExistingSourceFilePaths(string languageFilesRoot, string sourceKey)
    {
        if (string.IsNullOrEmpty(sourceKey) || !Directory.Exists(languageFilesRoot))
        {
            yield break;
        }

        foreach (var languageDirectory in Directory.GetDirectories(languageFilesRoot))
        {
            var filePath = GetSourceFilePath(languageFilesRoot, Path.GetFileName(languageDirectory), sourceKey);
            if (File.Exists(filePath))
            {
                yield return filePath;
            }
        }
    }
}
