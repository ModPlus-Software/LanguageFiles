namespace LangFilesEditor.Core.Abstractions;

using Models;

/// <summary>
/// Команды редактора.
/// </summary>
public interface IEditorCommands
{
    /// <summary>
    /// Сохраняет изменения на диск.
    /// </summary>
    /// <returns><see langword="false"/> при ошибках валидации или некорректных данных; иначе <see langword="true"/>.</returns>
    bool Save();

    /// <summary>
    /// Помечает entry для удаления из XML при сохранении. Пометка привязана к самим объектам,
    /// а не к их именам, поэтому переименования до сохранения её не сбивают.
    /// </summary>
    /// <param name="module">Модуль, содержащий удаляемую строку.</param>
    /// <param name="entry">Строка перевода, подлежащая удалению.</param>
    void TrackItemForRemoval(Module module, TranslationEntry entry);
}