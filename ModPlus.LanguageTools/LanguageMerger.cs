namespace ModPlus.LanguageTools;

using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Слияние (сборка) языковых файлов ModPlus: доменные файлы каждого языка склеиваются
/// в один файл <c>{язык}.xml</c> с версией пакета локализации в корневом элементе.
/// </summary>
/// <remarks>
/// Домены идут в порядке <see cref="LanguageFolder.DomainPrefixes"/>, перед каждым — комментарий
/// с именем домена; узлы модулей внутри домена сортируются по имени. Комментарии исходных
/// файлов в языковой файл не попадают: они лишь увеличивают его размер.
/// </remarks>
public static class LanguageMerger
{
    /// <summary>
    /// Собирает языковые файлы. Ранее собранные xml-файлы в папке назначения удаляются:
    /// язык, удалённый из источника, не должен остаться в папке
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="languages">Языки</param>
    /// <param name="outputDirectory">Папка назначения</param>
    /// <param name="version">Версия, записываемая в языковые файлы</param>
    /// <param name="log">Лог или <c>null</c></param>
    /// <returns>Собранные файлы</returns>
    /// <exception cref="LanguageToolsException">Не удалось очистить папку назначения</exception>
    public static IReadOnlyList<string> Merge(
        string root,
        IReadOnlyList<string> languages,
        string outputDirectory,
        Version version,
        ILanguageToolsLog log = null)
    {
        ArgumentNullException.ThrowIfNull(version);

        Directory.CreateDirectory(outputDirectory);
        log?.Info($"Папка языковых файлов: {outputDirectory}");
        log?.Info($"Версия локализации: {version}");

        ClearDirectory(outputDirectory, log);

        var builtFiles = new List<string>();

        foreach (var language in languages)
        {
            log?.Info($"Обработка языка {language}");

            var document = BuildLanguageDocument(root, language, version);
            var file = Path.Combine(outputDirectory, $"{language}.xml");
            Save(file, document);
            builtFiles.Add(file);

            log?.Info($"Создан файл {file}");
        }

        log?.Info($"Собрано языковых файлов: {builtFiles.Count}");

        return builtFiles;
    }

    /// <summary>
    /// Склеивает доменные файлы одного языка в единый документ языкового файла
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="language">Язык</param>
    /// <param name="version">Версия пакета локализации</param>
    public static XElement BuildLanguageDocument(string root, string language, Version version)
    {
        var document = new XElement("ModPlus");
        document.SetAttributeValue("Name", language);
        document.SetAttributeValue("Version", version.ToString());

        foreach (var domainPrefix in LanguageFolder.DomainPrefixes)
        {
            document.Add(new XComment(domainPrefix));

            var moduleNodes = LanguageFolder.GetDomainFiles(root, language, domainPrefix)
                .SelectMany(file => XElement.Load(file).Elements())
                .OrderBy(node => node.Name.LocalName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var moduleNode in moduleNodes)
            {
                moduleNode.DescendantNodes().Where(node => node.NodeType == XmlNodeType.Comment).Remove();
                document.Add(moduleNode);
            }
        }

        return document;
    }

    /// <summary>
    /// Возвращает версии, записанные в собранные языковые файлы
    /// </summary>
    /// <param name="files">Собранные языковые файлы</param>
    /// <exception cref="LanguageToolsException">Файла нет, версия не указана или различается</exception>
    public static Version GetBuiltVersion(IReadOnlyList<string> files)
    {
        var missing = files.Where(file => !File.Exists(file)).ToList();
        if (missing.Count > 0)
        {
            throw new LanguageToolsException(
                "Не собраны языковые файлы:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
        }

        var versions = files
            .Select(file => (File: file, Version: XElement.Load(file).Attribute("Version")?.Value))
            .ToList();

        var withoutVersion = versions.Where(v => string.IsNullOrEmpty(v.Version)).Select(v => v.File).ToList();
        if (withoutVersion.Count > 0)
        {
            throw new LanguageToolsException(
                "В языковых файлах не указана версия:" + Environment.NewLine + string.Join(Environment.NewLine, withoutVersion));
        }

        var distinct = versions.Select(v => v.Version).Distinct(StringComparer.Ordinal).ToList();
        if (distinct.Count > 1)
        {
            throw new LanguageToolsException(
                "Версии языковых файлов различаются:" + Environment.NewLine +
                string.Join(Environment.NewLine, versions.Select(v => $"{Path.GetFileName(v.File)}: {v.Version}")));
        }

        return Version.TryParse(distinct[0], out var version)
            ? version
            : throw new LanguageToolsException($"Версия языковых файлов «{distinct[0]}» не является номером версии");
    }

    private static void Save(string file, XElement document)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,

            // Явно, а не Environment.NewLine: на Linux (GitHub Actions) файл должен
            // получаться таким же, как на Windows
            NewLineChars = "\r\n",
        };

        using var writer = XmlWriter.Create(file, settings);
        document.Save(writer);
    }

    private static void ClearDirectory(string directory, ILanguageToolsLog log)
    {
        foreach (var file in Directory.GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exception)
            {
                throw new LanguageToolsException($"Не удалось удалить файл {file}: {exception.Message}", exception);
            }
        }

        log?.Info($"Папка {directory} очищена");
    }
}
