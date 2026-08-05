namespace LangFilesEditor.Services;

using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Models;

/// <summary>
/// Фабрика новых записей перевода и генератор имён ключей по шаблону.
/// </summary>
public class TranslationEntryService
{
    private readonly IReadOnlyList<string> _languages;

    /// <summary>
    /// Создаёт сервис с указанным списком языков для новых записей.
    /// </summary>
    /// <param name="languages">Имена языков в порядке колонок редактора.</param>
    public TranslationEntryService(IReadOnlyList<string> languages)
    {
        _languages = languages ?? throw new ArgumentNullException(nameof(languages));
    }

    /// <summary>
    /// Возвращает имя следующей записи, увеличивая числовой суффикс имени предыдущей записи.
    /// </summary>
    /// <param name="previousTranslationEntry">Предыдущая запись; если <c>null</c>, возвращается пустая строка.</param>
    /// <param name="existingEntries">Уже существующие записи модуля; при передаче имя подбирается уникальным.</param>
    /// <returns>Имя новой записи или пустая строка.</returns>
    public string GetNewTranslationEntryName(
        TranslationEntry previousTranslationEntry = null,
        IEnumerable<TranslationEntry> existingEntries = null)
    {
        if (previousTranslationEntry is null)
        {
            return string.Empty;
        }

        return GetNewTranslationEntryName(previousTranslationEntry.Name, existingEntries);
    }

    /// <summary>
    /// Возвращает имя следующей записи, увеличивая числовой суффикс в конце строки.
    /// Если суффикса нет — добавляет <c>1</c>. При переданном списке существующих записей
    /// продолжает инкремент, пока имя не станет уникальным (иначе «Add Row Below» сразу
    /// сталкивалось бы с именем следующей строки или копировало выбранную).
    /// </summary>
    /// <param name="previousTranslationEntryName">Имя предыдущей записи.</param>
    /// <param name="existingEntries">Уже существующие записи модуля; при передаче имя подбирается уникальным.</param>
    /// <returns>Имя с увеличенным суффиксом или пустая строка, если вход пуст.</returns>
    public string GetNewTranslationEntryName(
        string previousTranslationEntryName = null,
        IEnumerable<TranslationEntry> existingEntries = null)
    {
        if (string.IsNullOrEmpty(previousTranslationEntryName))
        {
            return string.Empty;
        }

        var candidate = IncrementTrailingNumber(previousTranslationEntryName);

        // Regex.Replace без совпадения возвращает ту же строку — без суффикса имя не менялось
        // и сразу давало дубликат выбранной записи.
        if (string.Equals(candidate, previousTranslationEntryName, StringComparison.Ordinal))
        {
            candidate = previousTranslationEntryName + "1";
        }

        if (existingEntries == null)
        {
            return candidate;
        }

        var taken = existingEntries
            .Select(entry => entry?.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToHashSet(StringComparer.Ordinal);

        while (taken.Contains(candidate))
        {
            var next = IncrementTrailingNumber(candidate);
            if (string.Equals(next, candidate, StringComparison.Ordinal))
            {
                candidate += "1";
                continue;
            }

            candidate = next;
        }

        return candidate;
    }

    /// <summary>
    /// Создаёт новую запись с автоматически сгенерированным именем на основе строки-шаблона.
    /// </summary>
    /// <param name="name">Имя или шаблон предыдущей записи.</param>
    /// <param name="valuesInOrder">Значения по языкам; если <c>null</c>, создаются пустые ячейки.</param>
    /// <param name="existingEntries">Уже существующие записи; при передаче имя подбирается уникальным.</param>
    /// <returns>Новая запись перевода.</returns>
    public TranslationEntry GetNewTranslationEntry(
        string name,
        List<string> valuesInOrder = null,
        IEnumerable<TranslationEntry> existingEntries = null) =>
        GetTranslationEntry(GetNewTranslationEntryName(name, existingEntries), valuesInOrder);

    /// <summary>
    /// Создаёт новую запись со следующим именем относительно предыдущей записи.
    /// </summary>
    /// <param name="previousTranslationEntry">Предыдущая запись для генерации имени.</param>
    /// <param name="valuesInOrder">Значения по языкам; если <c>null</c>, создаются пустые ячейки.</param>
    /// <param name="existingEntries">Уже существующие записи; при передаче имя подбирается уникальным.</param>
    /// <returns>Новая запись перевода.</returns>
    public TranslationEntry GetNewTranslationEntry(
        TranslationEntry previousTranslationEntry = null,
        List<string> valuesInOrder = null,
        IEnumerable<TranslationEntry> existingEntries = null)
    {
        return GetTranslationEntry(
            GetNewTranslationEntryName(previousTranslationEntry, existingEntries),
            valuesInOrder);
    }

    private static string IncrementTrailingNumber(string name) =>
        Regex.Replace(name, "\\d+$", match => (int.Parse(match.Value) + 1).ToString());

    /// <summary>
    /// Создаёт запись перевода с указанным именем и значениями по языкам.
    /// </summary>
    /// <param name="name">Имя ключа записи.</param>
    /// <param name="valuesInOrder">Значения по языкам в порядке списка языков сервиса; если <c>null</c>, создаются пустые ячейки для всех языков.</param>
    /// <returns>Новая запись перевода.</returns>
    public TranslationEntry GetTranslationEntry(string name, List<string> valuesInOrder = null)
    {
        var item = new TranslationEntry
        {
            Name = name
        };

        if (valuesInOrder == null)
        {
            foreach (var languageName in _languages)
            {
                item.Add(languageName, new ItemValue());
            }

            return item;
        }

        for (var i = 0; i < valuesInOrder.Count && i < _languages.Count; i++)
        {
            item.Add(_languages[i], new ItemValue { Value = valuesInOrder[i] });
        }

        return item;
    }
}