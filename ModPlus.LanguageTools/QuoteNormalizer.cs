namespace ModPlus.LanguageTools;

using System.Text;
using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Приведение кавычек в текстах перевода к прямой двойной кавычке <c>"</c>.
/// </summary>
/// <remarks>
/// Принято, что во всех языках используются только прямые двойные кавычки. Типографские
/// кавычки исправляются прямо в исходных файлах при сборке. Исправляются тексты строк
/// и значения атрибутов; комментарии не трогаются. Переписываются только файлы, в которых
/// что-то изменилось, и в том же формате, в котором их сохраняет редактор локализации
/// (<see cref="SourceXml.Save"/>).
/// </remarks>
public static class QuoteNormalizer
{
    /// <summary>
    /// Кавычки, заменяемые на прямую двойную
    /// </summary>
    public static IReadOnlyList<char> TypographicQuotes { get; } = ['«', '»', '“', '„', '”'];

    /// <summary>
    /// Есть ли в тексте типографские кавычки
    /// </summary>
    /// <param name="value">Текст</param>
    public static bool HasTypographicQuotes(string value) =>
        !string.IsNullOrEmpty(value) && value.AsSpan().IndexOfAny(['«', '»', '“', '„', '”']) >= 0;

    /// <summary>
    /// Заменяет типографские кавычки на прямую двойную
    /// </summary>
    /// <param name="value">Текст</param>
    public static string Normalize(string value)
    {
        if (!HasTypographicQuotes(value))
            return value;

        var builder = new StringBuilder(value);
        foreach (var quote in TypographicQuotes)
            builder.Replace(quote, '"');

        return builder.ToString();
    }

    /// <summary>
    /// Возвращает файлы, в текстах которых есть типографские кавычки
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="languages">Языки</param>
    public static IReadOnlyList<string> FindFiles(string root, IReadOnlyList<string> languages) =>
        languages
            .SelectMany(language => LanguageFolder.GetLanguageFiles(root, language))
            .Where(file => NeedsFix(file))
            .ToList();

    /// <summary>
    /// Исправляет кавычки в исходных файлах
    /// </summary>
    /// <param name="root">Папка с файлами локализации</param>
    /// <param name="languages">Языки</param>
    /// <param name="log">Лог или <c>null</c></param>
    /// <returns>Исправленные файлы</returns>
    public static IReadOnlyList<string> FixFiles(string root, IReadOnlyList<string> languages, ILanguageToolsLog log = null)
    {
        var fixedFiles = new List<string>();

        foreach (var file in FindFiles(root, languages))
        {
            var document = XElement.Load(file);
            var count = NormalizeElement(document);
            SourceXml.Save(file, document);
            fixedFiles.Add(file);

            log?.Info($"Исправлены кавычки ({count}): {Path.GetRelativePath(root, file)}");
        }

        return fixedFiles;
    }

    /// <summary>
    /// Исправляет кавычки в текстах и значениях атрибутов элемента и всех вложенных элементов
    /// </summary>
    /// <param name="element">Элемент</param>
    /// <returns>Число исправленных значений</returns>
    public static int NormalizeElement(XElement element)
    {
        var count = 0;

        foreach (var current in element.DescendantsAndSelf())
        {
            foreach (var attribute in current.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || !HasTypographicQuotes(attribute.Value))
                    continue;

                attribute.Value = Normalize(attribute.Value);
                count++;
            }

            foreach (var text in current.Nodes().OfType<XText>())
            {
                if (!HasTypographicQuotes(text.Value))
                    continue;

                text.Value = Normalize(text.Value);
                count++;
            }
        }

        return count;
    }

    private static bool NeedsFix(string file)
    {
        XElement document;
        try
        {
            document = XElement.Load(file);
        }
        catch (XmlException)
        {
            // Некорректный файл — забота проверки формата, а не исправления кавычек
            return false;
        }

        return document.DescendantsAndSelf().Any(element =>
            element.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration && HasTypographicQuotes(attribute.Value)) ||
            element.Nodes().OfType<XText>().Any(text => HasTypographicQuotes(text.Value)));
    }
}
