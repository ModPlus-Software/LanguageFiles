namespace ModPlus.LanguageTools;

using System.Net.Http;
using System.Xml.Linq;

/// <summary>
/// Файл <c>Langs.xml</c>: список языков с версией пакета локализации.
/// </summary>
/// <remarks>
/// Лежит в папке с файлами локализации и выгружается вместе с языковыми файлами:
/// по нему клиент понимает, что переводы обновились. Версия у всех языков одна.
/// <code>
/// &lt;Langs&gt;
///   &lt;lang Name="ru-RU" Version="15.0.23.0"&gt;&lt;/lang&gt;
///   ...
/// &lt;/Langs&gt;
/// </code>
/// </remarks>
public static class LanguagesVersionFile
{
    /// <summary>
    /// Адрес хранилища по умолчанию
    /// </summary>
    public const string DefaultStorageUrl = "https://storage.modplus.org/";

    /// <summary>
    /// Путь к опубликованному <c>Langs.xml</c> относительно адреса хранилища
    /// </summary>
    public const string RemoteRelativePath = "Languages/Langs.xml";

    /// <summary>
    /// Имя элемента языка
    /// </summary>
    private const string LangElementName = "lang";

    /// <summary>
    /// Один общий HttpClient на процесс: создание нового на каждый запрос
    /// приводит к исчерпанию сокетов
    /// </summary>
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>
    /// Читает версию из <c>Langs.xml</c> папки с файлами локализации
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <exception cref="LanguageToolsException">Файла нет, версия не указана, не разбирается
    /// или различается у языков</exception>
    public static Version Read(string root)
    {
        var file = LanguageFolder.GetVersionFilePath(root);
        if (!File.Exists(file))
            throw new LanguageToolsException($"Не найден файл версии локализации {file}");

        try
        {
            return Parse(File.ReadAllText(file));
        }
        catch (LanguageToolsException exception)
        {
            throw new LanguageToolsException($"{file}: {exception.Message}");
        }
    }

    /// <summary>
    /// Пытается прочитать версию из <c>Langs.xml</c> папки с файлами локализации
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <returns>Версия или <c>null</c>, если прочитать не удалось</returns>
    public static Version TryRead(string root)
    {
        try
        {
            return Read(root);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Разбирает содержимое <c>Langs.xml</c> и возвращает версию
    /// </summary>
    /// <param name="content">Содержимое файла</param>
    /// <exception cref="LanguageToolsException">Версия не указана, не разбирается или различается у языков</exception>
    public static Version Parse(string content)
    {
        XElement root;
        try
        {
            root = XElement.Parse(content);
        }
        catch (Exception exception)
        {
            throw new LanguageToolsException($"Некорректный xml: {exception.Message}", exception);
        }

        var values = root.Elements(LangElementName)
            .Select(element => element.Attribute("Version")?.Value?.Trim())
            .ToList();

        if (values.Count == 0)
            throw new LanguageToolsException("Нет ни одного языка");

        if (values.Any(string.IsNullOrEmpty))
            throw new LanguageToolsException("Не у всех языков указана версия");

        var distinct = values.Distinct(StringComparer.Ordinal).ToList();
        if (distinct.Count > 1)
            throw new LanguageToolsException($"У языков разные версии: {string.Join(", ", distinct)}");

        return Version.TryParse(distinct[0], out var version)
            ? version
            : throw new LanguageToolsException($"Версия «{distinct[0]}» не является номером версии");
    }

    /// <summary>
    /// Записывает версию в <c>Langs.xml</c> и приводит список языков в соответствие с папками языков:
    /// у имеющихся записей проставляется версия, недостающие языки добавляются в конец,
    /// записи языков, которых больше нет, удаляются. Если файла нет — он создаётся
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="languages">Имена языков (<see cref="LanguageFolder.GetLanguageNames"/>)</param>
    /// <param name="version">Версия</param>
    /// <param name="log">Лог или <c>null</c></param>
    public static void Write(string root, IReadOnlyList<string> languages, Version version, ILanguageToolsLog log = null)
    {
        var file = LanguageFolder.GetVersionFilePath(root);

        XElement document;
        if (File.Exists(file))
        {
            document = XElement.Load(file);
        }
        else
        {
            log?.Info($"Файл {file} отсутствует — будет создан");
            document = new XElement("Langs");
        }

        var languageSet = new HashSet<string>(languages, StringComparer.OrdinalIgnoreCase);

        foreach (var element in document.Elements(LangElementName).ToList())
        {
            var name = element.Attribute("Name")?.Value;
            if (name != null && languageSet.Contains(name))
                continue;

            log?.Info($"Язык {name} удалён из {LanguageFolder.VersionFileName}: нет папки языка");
            element.Remove();
        }

        var existing = new HashSet<string>(
            document.Elements(LangElementName).Select(e => e.Attribute("Name")!.Value),
            StringComparer.OrdinalIgnoreCase);

        foreach (var language in languages.Where(language => !existing.Contains(language)))
        {
            log?.Info($"Язык {language} добавлен в {LanguageFolder.VersionFileName}");

            // Пустая строка внутри элемента сохраняет прежний вид записи <lang ...></lang>
            document.Add(new XElement(LangElementName, new XAttribute("Name", language), string.Empty));
        }

        foreach (var element in document.Elements(LangElementName))
            element.SetAttributeValue("Version", version.ToString());

        document.Save(file);

        log?.Info($"В {file} записана версия {version}");
    }

    /// <summary>
    /// Загружает версию локализации, опубликованную в хранилище
    /// </summary>
    /// <param name="storageUrl">Адрес хранилища (по умолчанию <see cref="DefaultStorageUrl"/>)</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <exception cref="LanguageToolsException">Не удалось загрузить или разобрать файл</exception>
    public static async Task<Version> ReadRemoteAsync(string storageUrl = null, CancellationToken cancellationToken = default)
    {
        var url = GetRemoteUrl(storageUrl);
        string content;
        try
        {
            content = await HttpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            throw new LanguageToolsException(
                $"Не удалось получить версию локализации из хранилища ({url}): {exception.Message}", exception);
        }

        try
        {
            return Parse(content);
        }
        catch (LanguageToolsException exception)
        {
            throw new LanguageToolsException($"{url}: {exception.Message}");
        }
    }

    /// <summary>
    /// Возвращает адрес опубликованного <c>Langs.xml</c>
    /// </summary>
    /// <param name="storageUrl">Адрес хранилища или <c>null</c></param>
    public static string GetRemoteUrl(string storageUrl) =>
        $"{(string.IsNullOrWhiteSpace(storageUrl) ? DefaultStorageUrl : storageUrl).TrimEnd('/')}/{RemoteRelativePath}";

    /// <summary>
    /// Возвращает следующую версию для публикации: большая из локальной и опубликованной,
    /// у которой вторая часть увеличена на единицу. Остальные части не меняются
    /// </summary>
    /// <example>15.0.23.0 → 15.1.23.0</example>
    /// <param name="localVersion">Версия из <c>Langs.xml</c> репозитория</param>
    /// <param name="remoteVersion">Версия в хранилище</param>
    public static Version GetNextVersion(Version localVersion, Version remoteVersion)
    {
        if (localVersion == null && remoteVersion == null)
            throw new ArgumentException("Не задана ни одна версия");

        var baseVersion = localVersion == null
            ? remoteVersion
            : remoteVersion == null || localVersion >= remoteVersion ? localVersion : remoteVersion;

        var minor = Math.Max(baseVersion.Minor, 0) + 1;

        if (baseVersion.Revision >= 0)
            return new Version(baseVersion.Major, minor, baseVersion.Build, baseVersion.Revision);

        return baseVersion.Build >= 0
            ? new Version(baseVersion.Major, minor, baseVersion.Build)
            : new Version(baseVersion.Major, minor);
    }
}
