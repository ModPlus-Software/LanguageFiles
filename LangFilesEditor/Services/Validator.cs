namespace LangFilesEditor.Services;

using Models;

/// <summary>
/// Валидатор коллекций записей перевода: помечает записи с дубликатами имён,
/// дубликатами наборов значений и некорректными данными.
/// </summary>
public class Validator
{
    /// <summary>
    /// Валидирует записи перевода модуля и обновляет флаги ошибок и предупреждений.
    /// </summary>
    /// <param name="translationEntries">Коллекция записей items модуля.</param>
    /// <returns><c>true</c>, если хотя бы одна запись содержит ошибку (<see cref="EntryDiagnosticState.IsVisibleError"/>).</returns>
    public bool ValidateItems(ICollection<TranslationEntry> translationEntries) => Validate(translationEntries);

    /// <summary>
    /// Валидирует атрибуты (metadata) модуля и обновляет флаги ошибок и предупреждений.
    /// </summary>
    /// <param name="metadata">Коллекция атрибутов модуля.</param>
    /// <returns><c>true</c>, если хотя бы один атрибут содержит ошибку (<see cref="EntryDiagnosticState.IsVisibleError"/>).</returns>
    public bool ValidateAttributes(ICollection<TranslationEntry> metadata) => Validate(metadata);

    private static bool Validate(ICollection<TranslationEntry> items)
    {
        foreach (var item in items)
        {
            item.Validate();
        }

        MarkDuplicateNames(items);
        MarkDuplicateValues(items);
        return items.Any(i => i.DiagnosticState.IsVisibleError);
    }

    /// <summary>
    /// Помечает все записи, чьё имя встречается в коллекции более одного раза.
    /// </summary>
    private static void MarkDuplicateNames(ICollection<TranslationEntry> items)
    {
        foreach (var item in items)
        {
            item.DiagnosticState.HasDuplicateName = false;
        }

        foreach (var group in items.GroupBy(i => i.Name).Where(g => g.Count() > 1))
        {
            foreach (var item in group)
            {
                item.DiagnosticState.HasDuplicateName = true;
            }
        }
    }

    /// <summary>
    /// Помечает записи без комментария, у которых полностью совпадают наборы значений по всем языкам:
    /// такие записи почти всегда означают дублирование ключа.
    /// </summary>
    private static void MarkDuplicateValues(ICollection<TranslationEntry> items)
    {
        foreach (var item in items)
        {
            item.DiagnosticState.HasDuplicateValue = false;
        }

        var groupsWithSameValues = items
            .Where(i => string.IsNullOrEmpty(i.Comment))
            .GroupBy(BuildValuesSignature, StringComparer.Ordinal)
            .Where(g => g.Skip(1).Any());

        foreach (var group in groupsWithSameValues)
        {
            foreach (var item in group)
            {
                item.DiagnosticState.HasDuplicateValue = true;
            }
        }
    }

    /// <summary>
    /// Строит сигнатуру набора значений записи для сравнения: значения всех языков сортируются
    /// и склеиваются редким управляющим символом, который не встречается в текстах перевода.
    /// </summary>
    private static string BuildValuesSignature(TranslationEntry translationEntry)
    {
        var sortedValues = translationEntry.Values.Values
            .Select(v => v?.Value ?? string.Empty)
            .OrderBy(s => s, StringComparer.Ordinal);
        // Unit Separator (U+001F) — управляющий символ, не встречающийся в обычном тексте перевода.
        const char rareSeparator = '\u001F';

        return string.Join(rareSeparator, sortedValues);
    }
}