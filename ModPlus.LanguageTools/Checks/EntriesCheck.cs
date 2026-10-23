namespace ModPlus.LanguageTools.Checks;

using System.Xml.Linq;

/// <summary>
/// Проверка записей перевода по правилам <see cref="EntryRules"/> — тем же, по которым
/// редактор локализации подсвечивает ошибки и предупреждения.
/// </summary>
/// <remarks>
/// Записи модуля собираются по всем языкам: строки — дочерние элементы узла модуля,
/// атрибуты — атрибуты узла модуля. Строки и атрибуты проверяются раздельно.
/// Дубликаты имён ищутся прямо в файле каждого языка: при чтении в редактор повторяющийся
/// элемент молча затирает предыдущий, и в редакторе такой дубликат не виден.
/// </remarks>
internal static class EntriesCheck
{
    private const string Category = "Записи";
    private const string ItemKind = "строка";
    private const string AttributeKind = "атрибут";

    /// <summary>
    /// Выполняет проверку
    /// </summary>
    /// <param name="sources">Загруженные файлы</param>
    /// <param name="issues">Список, в который добавляются найденные проблемы</param>
    public static void Run(SourceSet sources, List<CheckIssue> issues)
    {
        foreach (var (sourceKey, documents) in sources.Sources)
        {
            var orderedDocuments = sources.Languages
                .Where(documents.ContainsKey)
                .Select(language => (Language: language, Document: documents[language]))
                .ToList();

            foreach (var moduleName in CollectModuleNames(orderedDocuments))
            {
                var location = $"{sourceKey}, узел «{moduleName}»";
                var items = new List<Entry>();
                var attributes = new List<Entry>();
                var itemIndex = new Dictionary<string, Entry>(StringComparer.Ordinal);
                var attributeIndex = new Dictionary<string, Entry>(StringComparer.Ordinal);

                foreach (var (language, document) in orderedDocuments)
                {
                    var moduleNode = document.Root.Element(moduleName);
                    if (moduleNode == null)
                        continue;

                    ReadAttributes(moduleNode, language, document.File, attributes, attributeIndex);
                    ReadItems(moduleNode, language, document.File, items, itemIndex, location, issues);
                }

                Validate(location, AttributeKind, attributes, issues);
                Validate(location, ItemKind, items, issues);
            }
        }
    }

    private static IEnumerable<string> CollectModuleNames(IEnumerable<(string Language, SourceDocument Document)> documents)
    {
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (_, document) in documents)
        {
            foreach (var element in document.Root.Elements())
            {
                if (seen.Add(element.Name.LocalName))
                    names.Add(element.Name.LocalName);
            }
        }

        return names;
    }

    private static void ReadAttributes(
        XElement moduleNode,
        string language,
        string file,
        List<Entry> target,
        Dictionary<string, Entry> index)
    {
        foreach (var attribute in moduleNode.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
                continue;

            var entry = GetOrCreate(target, index, attribute.Name.LocalName);
            entry.Set(language, attribute.Value, file, SourceSet.GetLine(attribute) ?? SourceSet.GetLine(moduleNode));
        }
    }

    private static void ReadItems(
        XElement moduleNode,
        string language,
        string file,
        List<Entry> target,
        Dictionary<string, Entry> index,
        string location,
        List<CheckIssue> issues)
    {
        var namesInFile = new HashSet<string>(StringComparer.Ordinal);

        foreach (var element in moduleNode.Elements())
        {
            var name = element.Name.LocalName;
            var entry = GetOrCreate(target, index, name);

            if (!namesInFile.Add(name))
            {
                issues.Add(new CheckIssue(
                    CheckSeverity.Error,
                    Category,
                    $"{location}: ({ItemKind}) {name}: дубликат имени ключа ({language})",
                    file,
                    SourceSet.GetLine(element)));
            }

            if (string.IsNullOrEmpty(entry.Comment) && element.PreviousNode is XComment comment)
                entry.Comment = comment.Value;

            entry.Set(language, element.Value, file, SourceSet.GetLine(element));
        }
    }

    private static void Validate(string location, string kind, List<Entry> entries, List<CheckIssue> issues)
    {
        foreach (var entry in entries)
        {
            var reasons = new List<string>();

            if (EntryRules.IsEmptyName(entry.Name))
                reasons.Add("пустое имя ключа");
            else if (EntryRules.StartsWithDigit(entry.Name))
                reasons.Add("имя ключа не должно начинаться с цифры");

            var emptyLanguages = entry.Values
                .Where(pair => EntryRules.IsEmptyValue(pair.Value.Value))
                .Select(pair => pair.Key)
                .ToList();
            if (emptyLanguages.Count > 0)
                reasons.Add($"пустое значение перевода ({string.Join(", ", emptyLanguages)})");

            if (reasons.Count == 0)
                continue;

            var place = emptyLanguages.Count > 0 ? entry.Values[emptyLanguages[0]] : entry.Values.Values.First();
            issues.Add(new CheckIssue(
                CheckSeverity.Error,
                Category,
                $"{location}: ({kind}) {DisplayName(entry)}: {string.Join("; ", reasons)}",
                place.File,
                place.Line));
        }

        var duplicateValues = EntryRules.FindDuplicateValues(
            entries,
            entry => entry.Comment,
            entry => entry.Values.Values.Select(value => value.Value));

        foreach (var entry in duplicateValues)
        {
            var place = entry.Values.Values.First();
            issues.Add(new CheckIssue(
                CheckSeverity.Warning,
                Category,
                $"{location}: ({kind}) {DisplayName(entry)}: дубликат набора значений перевода",
                place.File,
                place.Line));
        }
    }

    private static string DisplayName(Entry entry) => string.IsNullOrEmpty(entry.Name) ? "<без имени>" : entry.Name;

    private static Entry GetOrCreate(List<Entry> target, Dictionary<string, Entry> index, string name)
    {
        if (index.TryGetValue(name, out var entry))
            return entry;

        entry = new Entry(name);
        target.Add(entry);
        index[name] = entry;
        return entry;
    }

    /// <summary>
    /// Запись перевода: ключ со значениями по языкам
    /// </summary>
    private sealed class Entry
    {
        public Entry(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Comment { get; set; }

        /// <summary>
        /// Значения по языкам в порядке языков. При повторе ключа в файле, как и в редакторе,
        /// значение замещается последним
        /// </summary>
        public Dictionary<string, EntryValue> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Set(string language, string value, string file, int? line) =>
            Values[language] = new EntryValue(value, file, line);
    }

    private sealed record EntryValue(string Value, string File, int? Line);
}
