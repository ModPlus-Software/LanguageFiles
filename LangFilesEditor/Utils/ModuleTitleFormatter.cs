namespace LangFilesEditor.Utils;

using System.Text;

// todo: переработать здесь методы нужно
/// <summary>
/// Форматирование длинных имён модулей для отображения в UI с переносами по границам PascalCase-слов.
/// </summary>
public static class ModuleTitleFormatter
{
    /// <summary>
    /// Максимальная длина одной строки заголовка по умолчанию.
    /// </summary>
    private const int DefaultMaxCharsPerLine = 24;
    
    /// <summary>
    /// Переносит длинные имена названий модулей для корректного отображения в UI. todo: но здесь тоже вопрос как переносит...
    /// </summary>
    /// <param name="title">Исходный заголовок модуля, возможно, с разделителем « — » между domain и module.</param>
    /// <param name="maxCharsPerLine">Максимальное количество символов в одной строке.</param>
    /// <returns>Заголовок в формате строк с максимальной шириной каждый <see cref="DefaultMaxCharsPerLine"/>.</returns>
    public static string WrapForDisplay(string title, int maxCharsPerLine = DefaultMaxCharsPerLine)
    {
        if (string.IsNullOrEmpty(title) || title.Length <= maxCharsPerLine)
        {
            return title;
        }
        
        var separatorIndex = title.IndexOf(Constants.DomainNamesSeparator, StringComparison.Ordinal);
        
        if (separatorIndex >= 0)
        {
            var left = title[..separatorIndex];
            var right = title[(separatorIndex + Constants.DomainNamesSeparator.Length)..];
            return $"{WrapForDisplay(left, maxCharsPerLine)}{Constants.DomainNamesSeparator}{WrapForDisplay(right, maxCharsPerLine)}";
        }
        
        var parts = SplitPascalCaseParts(title);
        if (parts.Count == 0)
        {
            return WrapFixedWidth(title, maxCharsPerLine);
        }
        
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var part in parts)
        {
            if (current.Length == 0)
            {
                if (part.Length <= maxCharsPerLine)
                {
                    current.Append(part);
                    continue;
                }
                
                lines.AddRange(SplitFixedWidth(part, maxCharsPerLine));
                continue;
            }
            
            if (current.Length + part.Length <= maxCharsPerLine)
            {
                current.Append(part);
                continue;
            }
            
            lines.Add(current.ToString());
            current.Clear();
            
            if (part.Length <= maxCharsPerLine)
            {
                current.Append(part);
                continue;
            }
            
            lines.AddRange(SplitFixedWidth(part, maxCharsPerLine));
        }
        
        if (current.Length > 0)
        {
            lines.Add(current.ToString());
        }
        
        return string.Join(Environment.NewLine, lines);
    }
    
    private static string WrapFixedWidth(string value, int maxCharsPerLine) =>
        string.Join(Environment.NewLine, SplitFixedWidth(value, maxCharsPerLine));
    
    private static IEnumerable<string> SplitFixedWidth(string segment, int maxCharsPerLine)
    {
        for (var i = 0; i < segment.Length; i += maxCharsPerLine)
        {
            var length = Math.Min(maxCharsPerLine, segment.Length - i);
            yield return segment.Substring(i, length);
        }
    }
    
    private static List<string> SplitPascalCaseParts(string value)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        foreach (var ch in value)
        {
            if (current.Length > 0 && char.IsUpper(ch) && !char.IsUpper(current[^1]))
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else if (current.Length > 0
                     && char.IsUpper(current[^1])
                     && char.IsLower(ch)
                     && current.Length > 1)
            {
                var last = current[^1];
                current.Length--;
                parts.Add(current.ToString());
                current.Clear();
                current.Append(last).Append(ch);
                continue;
            }
            
            current.Append(ch);
        }
        
        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }
        
        return parts;
    }
}