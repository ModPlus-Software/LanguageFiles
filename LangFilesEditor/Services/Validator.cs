namespace LangFilesEditor.Services;

using Models;
using ModPlus.LanguageTools.Checks;

/// <summary>
/// Валидатор коллекций записей перевода: помечает записи с дубликатами имён,
/// дубликатами наборов значений и некорректными данными. Сами правила — в
/// <see cref="EntryRules"/> (общие с проверкой файлов при сборке и публикации).
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

        foreach (var item in EntryRules.FindDuplicateNames(items, i => i.Name))
        {
            item.DiagnosticState.HasDuplicateName = true;
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

        var duplicates = EntryRules.FindDuplicateValues(
            items,
            i => i.Comment,
            i => i.Values.Values.Select(v => v?.Value));

        foreach (var item in duplicates)
        {
            item.DiagnosticState.HasDuplicateValue = true;
        }
    }
}