namespace LangFilesEditor.Utils;

using System.Text.RegularExpressions;

/// <summary>
/// Утилиты разбора текстовых строк, содержащих один XML-подобный тег
/// (например, скопированных строк перевода). Работают со строками, а не с XML-деревом.
/// </summary>
public static class TagTextUtils
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
    /// Разделяет имя тега на базовую часть и числовой суффикс: <c>t12</c> → <c>t</c> и <c>12</c>.
    /// Для имени без суффикса возвращается само имя и <c>0</c>.
    /// </summary>
    /// <param name="tag">Полное имя тега.</param>
    /// <param name="value">Базовая часть имени без числового суффикса.</param>
    /// <param name="number">Числовой суффикс в конце имени тега; <c>0</c>, если суффикса нет.</param>
    public static void GetTagValueAndNumber(string tag, out string value, out int number)
    {
        number = 0;
        value = tag ?? string.Empty;
        if (value.Length == 0)
        {
            return;
        }

        var match = Regex.Match(value, "\\d+$");
        if (!match.Success)
        {
            return;
        }

        // Суффикс длиннее int трактуется как отсутствие номера: имя остаётся целиком.
        if (!int.TryParse(match.Value, out number))
        {
            number = 0;
            return;
        }

        value = value[..match.Index];
    }
}