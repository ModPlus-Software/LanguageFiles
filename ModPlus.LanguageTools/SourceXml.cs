namespace ModPlus.LanguageTools;

using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Запись исходного файла локализации на диск.
/// </summary>
/// <remarks>
/// Единый формат для редактора локализации и для исправления кавычек при сборке:
/// отступы, каждый атрибут с новой строки, переводы строк CRLF, UTF-8 с BOM, объявление xml.
/// </remarks>
public static class SourceXml
{
    /// <summary>
    /// Настройки форматирования исходных файлов
    /// </summary>
    public static XmlWriterSettings SaveSettings => new()
    {
        Indent = true,
        NewLineOnAttributes = true,

        // Явно, а не Environment.NewLine: на Linux (GitHub Actions) файл должен
        // получаться таким же, как на Windows
        NewLineChars = "\r\n",
    };

    /// <summary>
    /// Записывает исходный файл локализации
    /// </summary>
    /// <param name="file">Путь к файлу</param>
    /// <param name="document">Корневой элемент</param>
    public static void Save(string file, XElement document)
    {
        using var writer = XmlWriter.Create(file, SaveSettings);
        document.WriteTo(writer);
    }
}
