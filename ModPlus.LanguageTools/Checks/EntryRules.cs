namespace ModPlus.LanguageTools.Checks;

/// <summary>
/// Правила проверки записей перевода (строк и атрибутов модуля).
/// </summary>
/// <remarks>
/// Единственное место, где описаны правила: их используют и проверка файлов
/// (<see cref="EntriesCheck"/>), и подсветка записей в редакторе локализации.
/// <list type="bullet">
/// <item>ошибка — пустое имя ключа, имя с цифры, пустое значение перевода, дубликат имени;</item>
/// <item>предупреждение — у записей без комментария полностью совпадают наборы значений по всем языкам.</item>
/// </list>
/// </remarks>
public static class EntryRules
{
    /// <summary>
    /// Пустое имя ключа
    /// </summary>
    /// <param name="name">Имя ключа</param>
    public static bool IsEmptyName(string name) => string.IsNullOrEmpty(name);

    /// <summary>
    /// Имя ключа начинается с цифры
    /// </summary>
    /// <param name="name">Имя ключа</param>
    public static bool StartsWithDigit(string name) => !string.IsNullOrEmpty(name) && char.IsDigit(name[0]);

    /// <summary>
    /// Пустое значение перевода
    /// </summary>
    /// <param name="value">Значение</param>
    public static bool IsEmptyValue(string value) => string.IsNullOrEmpty(value);

    /// <summary>
    /// Содержит ли запись некорректные данные: пустое имя, имя с цифры или пустое значение хотя бы в одном языке
    /// </summary>
    /// <param name="name">Имя ключа</param>
    /// <param name="values">Значения по языкам (только языки, в которых запись есть)</param>
    public static bool HasIncorrectData(string name, IEnumerable<string> values) =>
        IsEmptyName(name) || StartsWithDigit(name) || values.Any(IsEmptyValue);

    /// <summary>
    /// Возвращает записи, чьё имя встречается в коллекции более одного раза
    /// </summary>
    /// <typeparam name="T">Тип записи</typeparam>
    /// <param name="entries">Записи</param>
    /// <param name="nameSelector">Имя записи</param>
    public static IReadOnlyCollection<T> FindDuplicateNames<T>(IEnumerable<T> entries, Func<T, string> nameSelector) =>
        entries
            .GroupBy(nameSelector)
            .Where(group => group.Skip(1).Any())
            .SelectMany(group => group)
            .ToList();

    /// <summary>
    /// Возвращает записи без комментария, у которых полностью совпадают наборы значений по всем языкам:
    /// такие записи почти всегда означают дублирование ключа
    /// </summary>
    /// <typeparam name="T">Тип записи</typeparam>
    /// <param name="entries">Записи</param>
    /// <param name="commentSelector">Комментарий записи</param>
    /// <param name="valuesSelector">Значения записи по языкам</param>
    public static IReadOnlyCollection<T> FindDuplicateValues<T>(
        IEnumerable<T> entries,
        Func<T, string> commentSelector,
        Func<T, IEnumerable<string>> valuesSelector) =>
        entries
            .Where(entry => string.IsNullOrEmpty(commentSelector(entry)))
            .GroupBy(entry => BuildValuesSignature(valuesSelector(entry)), StringComparer.Ordinal)
            .Where(group => group.Skip(1).Any())
            .SelectMany(group => group)
            .ToList();

    /// <summary>
    /// Строит сигнатуру набора значений записи для сравнения: значения всех языков сортируются
    /// и склеиваются редким управляющим символом, который не встречается в текстах перевода
    /// </summary>
    /// <param name="values">Значения по языкам</param>
    private static string BuildValuesSignature(IEnumerable<string> values)
    {
        // Unit Separator (U+001F) — управляющий символ, не встречающийся в обычном тексте перевода
        const char rareSeparator = '\u001F';

        return string.Join(
            rareSeparator,
            values.Select(value => value ?? string.Empty).OrderBy(value => value, StringComparer.Ordinal));
    }
}
