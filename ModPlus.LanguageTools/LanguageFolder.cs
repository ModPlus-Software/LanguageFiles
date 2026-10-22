namespace ModPlus.LanguageTools;

using System.Text.RegularExpressions;

/// <summary>
/// Устройство папки с файлами локализации (<c>LanguageFiles</c>).
/// </summary>
/// <remarks>
/// В папке лежат подпапки языков (<c>ru-RU</c>, <c>en-US</c>, <c>uz-Latn-UZ</c> и т.д.)
/// с доменными файлами и файл <c>Langs.xml</c> с версией пакета локализации.
/// <para>
/// В подпапке языка соседствуют два соглашения об именовании: общий файл домена
/// (<c>Revit.xml</c>, <c>Revit_Architecture.xml</c>) и вынесенный файл отдельного плагина
/// с суффиксом языка (<c>Revit_mprAlignViews_ru.xml</c>). Оба приводятся к одному
/// языконезависимому ключу источника (<c>Revit_mprAlignViews</c>).
/// </para>
/// <para>
/// Перечисление файлов всегда сортируется по имени без учёта регистра, а префиксы доменов
/// сравниваются без учёта регистра в коде, а не маской файловой системы: на Linux
/// (GitHub Actions) маска чувствительна к регистру, а порядок файлов произволен —
/// результат сборки должен совпадать с результатом на Windows.
/// </para>
/// </remarks>
public static class LanguageFolder
{
    /// <summary>
    /// Имя файла с версией пакета локализации и списком языков
    /// </summary>
    public const string VersionFileName = "Langs.xml";

    /// <summary>
    /// Имя общего домена (файл <c>Common.xml</c>)
    /// </summary>
    public const string CommonDomainName = "Common";

    /// <summary>
    /// Префиксы доменов в порядке склейки. Файлы, не подходящие ни под один префикс,
    /// в собранный языковой файл не попадают
    /// </summary>
    public static IReadOnlyList<string> DomainPrefixes { get; } = [CommonDomainName, "AutoCAD", "Revit", "Renga", "nanoCAD"];

    /// <summary>
    /// Имя подпапки языка: тег языка в формате <c>ru-RU</c>, <c>pt-BR</c>, <c>uz-Latn-UZ</c>.
    /// Подпапки с другими именами (служебные папки IDE и т.п.) языками не считаются
    /// </summary>
    private static readonly Regex LanguageFolderNameRegex = new(
        @"^[a-z]{2,3}(-[A-Za-z0-9]{2,8})+$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Возвращает путь к файлу версии <c>Langs.xml</c>
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    public static string GetVersionFilePath(string root) => Path.Combine(root, VersionFileName);

    /// <summary>
    /// Проверяет, что папка существует, и возвращает её полный путь
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <exception cref="LanguageToolsException">Папка не задана или не существует</exception>
    public static string EnsureRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new LanguageToolsException("Не задана папка с файлами локализации");

        if (!Directory.Exists(root))
            throw new LanguageToolsException($"Папка с файлами локализации не существует: {root}");

        return Path.GetFullPath(root);
    }

    /// <summary>
    /// Является ли имя папки тегом языка
    /// </summary>
    /// <param name="name">Имя папки</param>
    public static bool IsLanguageFolderName(string name) =>
        !string.IsNullOrEmpty(name) && LanguageFolderNameRegex.IsMatch(name);

    /// <summary>
    /// Возвращает имена языков — имена подпапок папки с файлами локализации в алфавитном порядке.
    /// Это единственный источник состава языков: и проверка, и сборка, и выгрузка, и <c>Langs.xml</c>
    /// работают с этим списком
    /// </summary>
    /// <remarks>
    /// Подпапки, имя которых не похоже на тег языка, пропускаются с сообщением в лог —
    /// опечатка в имени папки (<c>fr_FR</c>) не должна молча выпасть из сборки.
    /// </remarks>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="log">Лог или <c>null</c></param>
    /// <exception cref="LanguageToolsException">Папки нет или в ней нет подпапок языков</exception>
    public static IReadOnlyList<string> GetLanguageNames(string root, ILanguageToolsLog log = null)
    {
        root = EnsureRoot(root);
        var languages = new List<string>();

        foreach (var directory in Directory.GetDirectories(root))
        {
            var name = Path.GetFileName(directory);
            if (IsLanguageFolderName(name))
                languages.Add(name);
            else if (!name.StartsWith('.'))
                log?.Warning($"Папка «{name}» пропущена: имя не является тегом языка (например, ru-RU)");
        }

        if (languages.Count == 0)
            throw new LanguageToolsException($"В папке {root} нет подпапок языков");

        languages.Sort(StringComparer.OrdinalIgnoreCase);

        return languages;
    }

    /// <summary>
    /// Возвращает xml-файлы папки языка, отсортированные по имени
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="languageName">Имя языка</param>
    public static IReadOnlyList<string> GetLanguageFiles(string root, string languageName)
    {
        var directory = Path.Combine(root, languageName);
        if (!Directory.Exists(directory))
            return [];

        return Directory.EnumerateFiles(directory, "*.xml", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Возвращает файлы папки языка, относящиеся к домену (имя начинается с префикса домена),
    /// отсортированные по имени
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="languageName">Имя языка</param>
    /// <param name="domainPrefix">Префикс домена</param>
    public static IEnumerable<string> GetDomainFiles(string root, string languageName, string domainPrefix) =>
        GetLanguageFiles(root, languageName)
            .Where(file => Path.GetFileName(file).StartsWith(domainPrefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Попадает ли файл в сборку (имя начинается с одного из <see cref="DomainPrefixes"/>)
    /// </summary>
    /// <param name="file">Путь или имя файла</param>
    public static bool IsMergedFile(string file)
    {
        var name = Path.GetFileName(file);
        return DomainPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Возвращает суффикс имени файла для языка: <c>ru-RU</c> → <c>ru</c>
    /// </summary>
    /// <param name="languageName">Имя языка</param>
    public static string GetLanguageSuffix(string languageName)
    {
        if (string.IsNullOrEmpty(languageName))
            return string.Empty;

        var separatorIndex = languageName.IndexOf('-');
        return separatorIndex > 0 ? languageName[..separatorIndex] : languageName;
    }

    /// <summary>
    /// Приводит имя файла конкретного языка к языконезависимому ключу источника:
    /// <c>Revit_mprAlignViews_ru</c> → <c>Revit_mprAlignViews</c>. Имена без суффикса языка
    /// (<c>Revit</c>, <c>Revit_Architecture</c>) возвращаются без изменений
    /// </summary>
    /// <param name="fileNameWithoutExtension">Имя файла без расширения</param>
    /// <param name="languageName">Имя языка папки, из которой взят файл</param>
    public static string GetSourceKey(string fileNameWithoutExtension, string languageName)
    {
        if (string.IsNullOrEmpty(fileNameWithoutExtension))
            return fileNameWithoutExtension;

        var languageSuffix = GetLanguageSuffix(languageName);
        if (languageSuffix.Length == 0)
            return fileNameWithoutExtension;

        var suffix = $"_{languageSuffix}";

        return fileNameWithoutExtension.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? fileNameWithoutExtension[..^suffix.Length]
            : fileNameWithoutExtension;
    }

    /// <summary>
    /// Собирает файлы по языконезависимым ключам источников
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="languages">Имена языков</param>
    /// <param name="mergedOnly">Только файлы, попадающие в сборку</param>
    /// <returns>Словарь «ключ источника → язык → путь к файлу»</returns>
    public static Dictionary<string, Dictionary<string, string>> CollectSources(
        string root, IReadOnlyList<string> languages, bool mergedOnly)
    {
        var sources = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var language in languages)
        {
            foreach (var file in GetLanguageFiles(root, language))
            {
                if (mergedOnly && !IsMergedFile(file))
                    continue;

                var sourceKey = GetSourceKey(Path.GetFileNameWithoutExtension(file), language);

                if (!sources.TryGetValue(sourceKey, out var filesByLanguage))
                {
                    filesByLanguage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    sources[sourceKey] = filesByLanguage;
                }

                filesByLanguage[language] = file;
            }
        }

        return sources;
    }
}
