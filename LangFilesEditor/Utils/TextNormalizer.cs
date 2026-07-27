namespace LangFilesEditor.Utils;

/// <summary>
/// Нормализация текста перевода к каноничному виду на этапе обработки ввода.
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Приводит типографские кавычки («» “”) к обычной двойной кавычке.
    /// </summary>
    /// <param name="value">Исходный текст</param>
    /// <returns>Текст с нормализованными кавычками.</returns>
    public static string NormalizeQuotes(string value) =>
        value?
            .Replace("«", "\"")
            .Replace("»", "\"")
            .Replace("“", "\"")
            .Replace("”", "\"");
}