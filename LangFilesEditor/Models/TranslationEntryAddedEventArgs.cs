namespace LangFilesEditor.Models;

// todo: а оно вообще нужно? Можно мне кажется удалить если это лишннее. И дочерни классы соответствнно тоже.
/// <summary>
/// Аргументы события добавления <see cref="TranslationEntry"/> в модуль.
/// </summary>
public sealed class TranslationEntryAddedEventArgs : EventArgs
{
    /// <summary>
    /// Создаёт аргументы события добавления записи.
    /// </summary>
    /// <param name="entry">Добавленная запись перевода.</param>
    /// <param name="context">Контекст добавления (источник и прогресс загрузки).</param>
    public TranslationEntryAddedEventArgs(TranslationEntry entry, TranslationEntryAddContext context)
    {
        Entry = entry;
        Context = context;
    }
    
    /// <summary>
    /// Добавленная запись перевода.
    /// </summary>
    public TranslationEntry Entry { get; }
    
    /// <summary>
    /// Контекст добавления записи.
    /// </summary>
    public TranslationEntryAddContext Context { get; }
}