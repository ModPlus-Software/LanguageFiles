namespace ModPlus.LanguageTools.Checks;

using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Загруженные исходные файлы локализации, сгруппированные по ключу источника и языку.
/// </summary>
/// <remarks>
/// Каждый файл читается один раз и используется всеми проверками. Файл, который не удалось
/// разобрать, в набор не попадает — вместо него в <see cref="LoadIssues"/> записывается ошибка.
/// </remarks>
internal sealed class SourceSet
{
    private SourceSet(
        IReadOnlyList<string> languages,
        SortedDictionary<string, Dictionary<string, SourceDocument>> sources,
        IReadOnlyList<CheckIssue> loadIssues)
    {
        Languages = languages;
        Sources = sources;
        LoadIssues = loadIssues;
    }

    /// <summary>
    /// Языки
    /// </summary>
    public IReadOnlyList<string> Languages { get; }

    /// <summary>
    /// «Ключ источника → язык → документ», ключи по алфавиту
    /// </summary>
    public SortedDictionary<string, Dictionary<string, SourceDocument>> Sources { get; }

    /// <summary>
    /// Ошибки чтения файлов
    /// </summary>
    public IReadOnlyList<CheckIssue> LoadIssues { get; }

    /// <summary>
    /// Загружает все xml-файлы языков
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="languages">Языки</param>
    public static SourceSet Load(string root, IReadOnlyList<string> languages)
    {
        var sources = new SortedDictionary<string, Dictionary<string, SourceDocument>>(StringComparer.OrdinalIgnoreCase);
        var issues = new List<CheckIssue>();

        foreach (var (sourceKey, filesByLanguage) in LanguageFolder.CollectSources(root, languages, mergedOnly: false))
        {
            var documents = new Dictionary<string, SourceDocument>(StringComparer.OrdinalIgnoreCase);

            foreach (var (language, file) in filesByLanguage)
            {
                try
                {
                    documents[language] = new SourceDocument(file, XElement.Load(file, LoadOptions.SetLineInfo));
                }
                catch (XmlException exception)
                {
                    issues.Add(new CheckIssue(
                        CheckSeverity.Error,
                        "Формат xml",
                        $"{Path.GetFileName(file)}: файл не является корректным xml — {exception.Message}",
                        file,
                        exception.LineNumber > 0 ? exception.LineNumber : null));
                }
            }

            sources[sourceKey] = documents;
        }

        return new SourceSet(languages, sources, issues);
    }

    /// <summary>
    /// Возвращает номер строки элемента в файле или <c>null</c>
    /// </summary>
    /// <param name="node">Элемент</param>
    public static int? GetLine(XObject node) =>
        node is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : null;
}

/// <summary>
/// Загруженный исходный файл локализации
/// </summary>
/// <param name="File">Полный путь к файлу</param>
/// <param name="Root">Корневой элемент</param>
internal sealed record SourceDocument(string File, XElement Root);
