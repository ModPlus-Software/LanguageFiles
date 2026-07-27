namespace LangFilesEditor.Services.RepositoryServices;

using Models;

// todo: Этого класса быть не должно, вероятно. 
/// <summary>
/// Результат чтения entries модуля с диска (без мутации UI-модели).
/// </summary>
public sealed class ModuleTranslationData
{
    /// <summary>
    /// Создаёт контейнер с прочитанными атрибутами и записями модуля.
    /// </summary>
    /// <param name="metadata">Атрибуты XML-узла модуля.</param>
    /// <param name="items">Элементы перевода модуля.</param>
    public ModuleTranslationData(IReadOnlyList<TranslationEntry> metadata, IReadOnlyList<TranslationEntry> items)
    {
        Metadata = metadata;
        Items = items;
    }
    
    /// <summary>
    /// Атрибуты (metadata) модуля, прочитанные с диска.
    /// </summary>
    public IReadOnlyList<TranslationEntry> Metadata { get; }
    
    /// <summary>
    /// Записи перевода (items) модуля, прочитанные с диска.
    /// </summary>
    public IReadOnlyList<TranslationEntry> Items { get; }
}