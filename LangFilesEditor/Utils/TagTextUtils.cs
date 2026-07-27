namespace LangFilesEditor.Utils;

using System.Text.RegularExpressions;

/// <summary>
/// Утилиты разбора текстовых строк, содержащих один XML-подобный тег
/// (например, скопированных строк перевода). Работают со строками, а не с XML-деревом.
/// </summary>
public class TagTextUtils
{
    /// <summary>
    /// Удаляет открывающий и закрывающий теги из одной строки, оставляя только содержимое.
    /// </summary>
    /// <param name="row">Строка вида <c>&lt;Tag&gt;значение&lt;/Tag&gt;</c>.</param>
    /// <returns>Текст между тегами без обрамляющей разметки.</returns>
    public static string StripRowOfTag(string row)
    {
        var result = Regex.Replace(row.Trim(), "^<[^>]+>", string.Empty);
        result = Regex.Replace(result, "<[^>]+>$", string.Empty);
        return result;
    }
    
    /// <summary>
    /// Извлекает имя открывающего тега из строки.
    /// </summary>
    /// <param name="row">Строка с тегом.</param>
    /// <returns>Имя тега без угловых скобок или пустая строка, если тег не найден.</returns>
    public static string GetRowTagName(string row)
    {
        var match = Regex.Match(row.Trim(), "^<[^>]+>");
        if (!match.Success)
        {
            return string.Empty;
        }
        
        return match.Value.Substring(1, match.Value.Length - 2);
    }
    
    /// <summary>
    /// Разделяет имя тега на базовую часть и числовой суффикс.
    /// </summary>
    /// <param name="tag">Полное имя тега.</param>
    /// <param name="value">Базовая часть имени без числового суффикса.</param>
    /// <param name="number">Числовой суффикс в конце имени тега.</param>
    public static void GetTagValueAndNumber(string tag, out string value, out int number)
    {
        var match = Regex.Match(tag, "\\d+$");
        if (match.Success)
        {
            number = 1;
            value = tag;
            return;
        }
        
        int.TryParse(match.Value, out number);
        value = tag.Replace(match.Value, string.Empty);
    }
}