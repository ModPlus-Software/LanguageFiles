namespace ModPlus.LanguageTools.Checks;

using System.Xml.Linq;

/// <summary>
/// Проверка того, что файлы локализации всех языков описывают одно и то же:
/// одинаковый набор файлов, а внутри файла — одинаковые узлы, под-узлы и атрибуты.
/// </summary>
/// <remarks>
/// Языковые файлы собираются из десятков доменных файлов, и расхождение между языками
/// ничем себя не проявляет до тех пор, пока пользователь с «неполным» языком не откроет
/// плагин и не увидит пустую надпись. Дешевле сверить языки между собой до сборки.
/// <para>
/// Сравниваются файлы, попадающие в сборку (<see cref="LanguageFolder.DomainPrefixes"/>).
/// Проверка не прекращается на первой проблеме: собирается полный список. Отсутствующий
/// целиком файл не порождает лавину сообщений об отсутствующих узлах — язык без файла
/// из сравнения узлов исключается.
/// </para>
/// </remarks>
internal static class StructureCheck
{
    private const string FilesCategory = "Состав файлов";
    private const string NodesCategory = "Узлы";
    private const string SubNodesCategory = "Под-узлы";
    private const string AttributesCategory = "Атрибуты";

    /// <summary>
    /// Выполняет проверку
    /// </summary>
    /// <param name="sources">Загруженные файлы</param>
    /// <param name="issues">Список, в который добавляются найденные проблемы</param>
    public static void Run(SourceSet sources, List<CheckIssue> issues)
    {
        foreach (var (sourceKey, documents) in sources.Sources)
        {
            if (!LanguageFolder.IsMergedFile(sourceKey))
                continue;

            var missingLanguages = sources.Languages
                .Where(language => !documents.ContainsKey(language) && !HasUnreadableFile(sources, language, sourceKey))
                .ToList();
            if (missingLanguages.Count > 0)
            {
                issues.Add(Error(
                    FilesCategory,
                    $"{sourceKey}: файл отсутствует в языках {Join(missingLanguages)}",
                    documents.Values.FirstOrDefault()?.File));
            }

            // Сравнивать содержимое имеет смысл только при двух и более языках
            if (documents.Count > 1)
                CompareSource(sourceKey, documents, issues);
        }
    }

    /// <summary>
    /// Есть ли у языка файл источника, который не удалось прочитать: об этом уже есть ошибка,
    /// сообщать ещё и об отсутствии файла незачем
    /// </summary>
    private static bool HasUnreadableFile(SourceSet sources, string language, string sourceKey) =>
        sources.LoadIssues.Any(issue =>
            issue.File != null &&
            string.Equals(Path.GetFileName(Path.GetDirectoryName(issue.File)), language, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                LanguageFolder.GetSourceKey(Path.GetFileNameWithoutExtension(issue.File), language),
                sourceKey,
                StringComparison.OrdinalIgnoreCase));

    private static void CompareSource(
        string sourceKey,
        Dictionary<string, SourceDocument> documents,
        List<CheckIssue> issues)
    {
        var nodesByLanguage = documents.ToDictionary(
            pair => pair.Key,
            pair => IndexElements(pair.Value.Root),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (nodeName, missingLanguages) in FindDifferences(nodesByLanguage))
        {
            var present = SelectPresent(nodesByLanguage, nodeName).Values.First();
            issues.Add(Error(
                NodesCategory,
                $"{sourceKey}: узел «{nodeName}» отсутствует в языках {Join(missingLanguages)}",
                FindFile(documents, present),
                SourceSet.GetLine(present)));
        }

        foreach (var nodeName in GetAllNames(nodesByLanguage))
        {
            var nodes = SelectPresent(nodesByLanguage, nodeName);

            // Узел, который есть лишь в одном языке, уже отмечен выше: сравнивать его содержимое не с чем
            if (nodes.Count < 2)
                continue;

            CompareAttributes(nodes, documents, issues, $"{sourceKey}, узел «{nodeName}»");

            var subNodesByLanguage = nodes.ToDictionary(
                pair => pair.Key,
                pair => IndexElements(pair.Value),
                StringComparer.OrdinalIgnoreCase);

            foreach (var (subNodeName, missingLanguages) in FindDifferences(subNodesByLanguage))
            {
                var present = SelectPresent(subNodesByLanguage, subNodeName).Values.First();
                issues.Add(Error(
                    SubNodesCategory,
                    $"{sourceKey}, узел «{nodeName}»: под-узел «{subNodeName}» отсутствует в языках {Join(missingLanguages)}",
                    FindFile(documents, present),
                    SourceSet.GetLine(present)));
            }

            foreach (var subNodeName in GetAllNames(subNodesByLanguage))
            {
                var subNodes = SelectPresent(subNodesByLanguage, subNodeName);
                if (subNodes.Count < 2)
                    continue;

                CompareAttributes(
                    subNodes, documents, issues, $"{sourceKey}, узел «{nodeName}», под-узел «{subNodeName}»");
            }
        }
    }

    private static void CompareAttributes(
        Dictionary<string, XElement> elements,
        Dictionary<string, SourceDocument> documents,
        List<CheckIssue> issues,
        string location)
    {
        var attributesByLanguage = elements.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Attributes()
                .Where(attribute => !attribute.IsNamespaceDeclaration)
                .ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute, StringComparer.Ordinal),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (attributeName, missingLanguages) in FindDifferences(attributesByLanguage))
        {
            var element = elements.Values.First();
            issues.Add(Error(
                AttributesCategory,
                $"{location}: атрибут «{attributeName}» отсутствует в языках {Join(missingLanguages)}",
                FindFile(documents, element),
                SourceSet.GetLine(element)));
        }
    }

    /// <summary>
    /// Индексирует дочерние элементы по именам. Повторы имён отбрасываются: сравнение ведётся
    /// по составу имён, а не по числу узлов (повторы ловит <see cref="EntriesCheck"/>)
    /// </summary>
    private static Dictionary<string, XElement> IndexElements(XElement element)
    {
        var elements = new Dictionary<string, XElement>(StringComparer.Ordinal);

        foreach (var child in element.Elements())
            elements.TryAdd(child.Name.LocalName, child);

        return elements;
    }

    private static IEnumerable<string> GetAllNames<T>(Dictionary<string, Dictionary<string, T>> byLanguage) =>
        byLanguage.Values
            .SelectMany(names => names.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);

    private static IEnumerable<(string Name, List<string> MissingLanguages)> FindDifferences<T>(
        Dictionary<string, Dictionary<string, T>> byLanguage)
    {
        var languages = byLanguage.Keys.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var name in GetAllNames(byLanguage))
        {
            var missingLanguages = languages.Where(language => !byLanguage[language].ContainsKey(name)).ToList();
            if (missingLanguages.Count > 0)
                yield return (name, missingLanguages);
        }
    }

    private static Dictionary<string, T> SelectPresent<T>(
        Dictionary<string, Dictionary<string, T>> byLanguage, string name) =>
        byLanguage
            .Where(pair => pair.Value.ContainsKey(name))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(pair => pair.Key, pair => pair.Value[name], StringComparer.OrdinalIgnoreCase);

    private static string FindFile(Dictionary<string, SourceDocument> documents, XElement element)
    {
        var top = Top(element);
        return documents.Values.FirstOrDefault(document => ReferenceEquals(document.Root, top))?.File;
    }

    private static XElement Top(XElement element)
    {
        while (element.Parent != null)
            element = element.Parent;

        return element;
    }

    private static string Join(IEnumerable<string> languages) => string.Join(", ", languages);

    private static CheckIssue Error(string category, string message, string file = null, int? line = null) =>
        new(CheckSeverity.Error, category, message, file, line);
}
