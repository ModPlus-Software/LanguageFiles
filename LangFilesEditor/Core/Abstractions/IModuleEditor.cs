namespace LangFilesEditor.Core.Abstractions;

using Models;

/// <summary>
/// Интерфейс для изменения модулей.
/// </summary>
public interface IModuleEditor
{
    /// <summary>
    /// Возвращает существующий module или создаёт новый в domain с указанным исходным XML-файлом.
    /// </summary>
    /// <param name="domain">Domain, в котором ищется или создаётся модуль.</param>
    /// <param name="moduleName">Имя модуля.</param>
    /// <param name="sourceFileName">Имя исходного XML-файла модуля.</param>
    /// <returns>Существующий или только что созданный <see cref="Module"/>.</returns>
    Module GetOrCreateModule(Domain domain, string moduleName, string sourceFileName);

    /// <summary>
    /// Добавляет entry в module.
    /// </summary>
    /// <param name="module">Модуль, в который добавляется строка.</param>
    /// <param name="entry">Добавляемая строка перевода.</param>
    /// <param name="source">Источник добавления.</param>
    void AddTranslationEntry(Module module, TranslationEntry entry, TranslationEntryAddSource source);

    /// <summary>
    /// Добавляет items, не перезаписывая существующие.
    /// </summary>
    /// <param name="module">Модуль, в который выполняется добавление переводов.</param>
    /// <param name="entries">Строки перевода для добавления или частичного обновления существующих.</param>
    void MergeTranslationEntries(Module module, IReadOnlyList<TranslationEntry> entries);
}