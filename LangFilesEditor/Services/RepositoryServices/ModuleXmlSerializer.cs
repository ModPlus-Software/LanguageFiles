namespace LangFilesEditor.Services.RepositoryServices;

using System.Xml;
using System.Xml.Linq;
using Models;
using Utils;

/// <summary>
/// Чтение и запись узла модуля в XML-файле локализации: атрибуты (metadata),
/// дочерние элементы (items) и комментарии к ним.
/// </summary>
internal static class ModuleXmlSerializer
{
    /// <summary>
    /// Настройки форматирования при записи XML на диск.
    /// </summary>
    public static readonly XmlWriterSettings SaveSettings = new()
    {
        Indent = true,
        NewLineOnAttributes = true
    };

    /// <summary>
    /// Читает атрибуты XML-узла модуля в коллекцию записей metadata.
    /// </summary>
    /// <param name="moduleNode">XML-узел модуля.</param>
    /// <param name="target">Целевая коллекция metadata.</param>
    /// <param name="index">Индекс уже созданных записей по имени; общий для всех языков одного модуля.</param>
    /// <param name="languageName">Код языка для значения атрибута.</param>
    public static void ReadMetadata(
        XElement moduleNode,
        ICollection<TranslationEntry> target,
        IDictionary<string, TranslationEntry> index,
        string languageName)
    {
        foreach (var attribute in moduleNode.Attributes())
        {
            if (attribute.IsNamespaceDeclaration)
            {
                continue;
            }

            var entry = FindOrCreateEntry(target, index, attribute.Name.LocalName);
            entry.Add(languageName, new ItemValue { Value = attribute.Value });
        }
    }

    /// <summary>
    /// Читает дочерние элементы XML-узла модуля в коллекцию записей items.
    /// </summary>
    /// <param name="moduleNode">XML-узел модуля.</param>
    /// <param name="target">Целевая коллекция items.</param>
    /// <param name="index">Индекс уже созданных записей по имени; общий для всех языков одного модуля.</param>
    /// <param name="languageName">Код языка для значения элемента.</param>
    public static void ReadItems(
        XElement moduleNode,
        ICollection<TranslationEntry> target,
        IDictionary<string, TranslationEntry> index,
        string languageName)
    {
        foreach (var element in moduleNode.Elements())
        {
            var entry = FindOrCreateEntry(target, index, element.Name.LocalName);
            if (string.IsNullOrEmpty(entry.Comment) && element.PreviousNode is XComment comment)
            {
                entry.Comment = comment.Value;
            }

            entry.Add(languageName, new ItemValue { Value = element.Value });
        }
    }

    /// <summary>
    /// Записывает изменённые атрибуты metadata в XML-узел модуля.
    /// </summary>
    /// <param name="moduleNode">XML-узел модуля.</param>
    /// <param name="metadata">Коллекция атрибутов для записи.</param>
    /// <param name="languageName">Код языка, значение которого записывается.</param>
    /// <returns><c>true</c>, если документ был изменён.</returns>
    public static bool WriteMetadata(XElement moduleNode, IEnumerable<TranslationEntry> metadata, string languageName)
    {
        var modified = false;
        foreach (var entry in metadata)
        {
            if (entry.DiagnosticState.HasIncorrectData || !TryGetLanguageValue(entry, languageName, out var value))
            {
                continue;
            }

            var currentValue = (string)moduleNode.Attribute(entry.Name) ?? string.Empty;
            if (currentValue == value)
            {
                continue;
            }

            moduleNode.SetAttributeValue(entry.Name, value);
            modified = true;
        }

        return modified;
    }

    /// <summary>
    /// Записывает изменённые элементы items в XML-узел модуля.
    /// </summary>
    /// <param name="moduleNode">XML-узел модуля.</param>
    /// <param name="items">Коллекция записей для записи.</param>
    /// <param name="languageName">Код языка, значение которого записывается.</param>
    /// <returns><c>true</c>, если документ был изменён.</returns>
    public static bool WriteItems(XElement moduleNode, IEnumerable<TranslationEntry> items, string languageName)
    {
        var modified = false;
        XElement previousItemNode = null;
        foreach (var entry in items)
        {
            if (entry.DiagnosticState.HasIncorrectData || !TryGetLanguageValue(entry, languageName, out var value))
            {
                continue;
            }

            var itemNode = moduleNode.Element(entry.Name);
            if (itemNode == null)
            {
                itemNode = new XElement(entry.Name);
                if (previousItemNode == null)
                {
                    moduleNode.AddFirst(itemNode);
                }
                else
                {
                    previousItemNode.AddAfterSelf(itemNode);
                }

                modified = true;
            }

            previousItemNode = itemNode;
            if (itemNode.Value != value)
            {
                itemNode.SetValue(value);
                modified = true;
            }

            modified |= WriteItemComment(itemNode, entry.Comment);
        }

        return modified;
    }

    /// <summary>
    /// Записывает или обновляет XML-комментарий перед элементом item.
    /// </summary>
    /// <param name="itemNode">XML-узел элемента перевода.</param>
    /// <param name="comment">Текст комментария. Пустое значение удаляет существующий комментарий.</param>
    /// <returns><c>true</c>, если комментарий был добавлен, изменён или удалён.</returns>
    public static bool WriteItemComment(XElement itemNode, string comment)
    {
        if (string.IsNullOrEmpty(comment))
        {
            // Пустой комментарий — это снятие пометки, а не «нечего писать». Без удаления
            // узла метка осталась бы в файле и вернулась бы в запись при следующей загрузке.
            // Удаляется только сама пометка: перед элементом может стоять чужой комментарий —
            // граница диапазона «todo remove after X start/end» или осиротевший комментарий
            // от удалённого ранее элемента, и терять их нельзя.
            if (itemNode.PreviousNode is not XComment obsoleteComment
                || !DeletionMarker.IsMarked(obsoleteComment.Value))
            {
                return false;
            }

            obsoleteComment.Remove();
            return true;
        }

        if (itemNode.PreviousNode is XComment existingComment)
        {
            if (existingComment.Value == comment)
            {
                return false;
            }

            existingComment.Value = comment;
            return true;
        }

        itemNode.AddBeforeSelf(new XComment(comment));
        return true;
    }

    /// <summary>
    /// Удаляет элементы items с указанными именами из XML-узла модуля.
    /// </summary>
    /// <param name="moduleNode">XML-узел модуля.</param>
    /// <param name="itemNames">Имена удаляемых элементов.</param>
    /// <returns><c>true</c>, если хотя бы один элемент был удалён.</returns>
    public static bool RemoveItems(XElement moduleNode, IEnumerable<string> itemNames)
    {
        var modified = false;
        foreach (var itemName in itemNames)
        {
            if (moduleNode.Element(itemName) is not { } itemNode)
            {
                continue;
            }

            itemNode.Remove();
            modified = true;
        }

        return modified;
    }

    /// <summary>
    /// Записывает XML-документ на диск с форматированием <see cref="SaveSettings"/>.
    /// </summary>
    /// <param name="filePath">Путь к целевому файлу.</param>
    /// <param name="document">Корневой XML-элемент для записи.</param>
    public static void WriteDocument(string filePath, XElement document)
    {
        using var writer = XmlWriter.Create(filePath, SaveSettings);
        document.WriteTo(writer);
    }

    /// <summary>
    /// Пытается получить значение перевода записи для указанного языка.
    /// </summary>
    /// <param name="entry">Запись перевода.</param>
    /// <param name="languageName">Код языка.</param>
    /// <param name="value">Найденное значение или пустая строка.</param>
    /// <returns><c>true</c>, если значение для языка существует.</returns>
    public static bool TryGetLanguageValue(TranslationEntry entry, string languageName, out string value)
    {
        if (entry.Values.TryGetValue(languageName, out var itemValue))
        {
            value = itemValue.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static TranslationEntry FindOrCreateEntry(
        ICollection<TranslationEntry> entries,
        IDictionary<string, TranslationEntry> index,
        string name)
    {
        if (index.TryGetValue(name, out var existing))
        {
            return existing;
        }

        existing = new TranslationEntry { Name = name };
        entries.Add(existing);
        index[name] = existing;
        return existing;
    }
}