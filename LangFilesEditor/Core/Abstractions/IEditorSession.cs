namespace LangFilesEditor.Core.Abstractions;

using System.Collections.ObjectModel;
using System.ComponentModel;
using Models;

/// <summary>
/// Сессия данных редактора: языки, домены, загрузка и сохранение.
/// Презентационное состояние (выбор, вкладки, результатные режимы) живёт отдельно —
/// см. <see cref="IEditorWorkspace"/>.
/// </summary>
public interface IEditorSession : INotifyPropertyChanged, IEditorCommands
{
    // todo: это неплохо, но вот вопрос как передаётся
    /// <summary>
    /// Коды языков проекта.
    /// </summary>
    IReadOnlyList<string> Languages { get; }
    
    /// <summary>
    /// Группы локализации.
    /// </summary>
    ObservableCollection<Domain> Domains { get; }
    
    /// <summary>
    /// В процессе ли длительная операция. Детали прогресса (список операций, доли, сообщения)
    /// UI получает напрямую у трекера операций, а не через сессию.
    /// </summary>
    bool IsOperationInProgress { get; }
    
    /// <summary>
    /// Загружает entries для модулей области поиска.
    /// </summary>
    /// <param name="modules">Модули, для которых требуется догрузить строки перевода.</param>
    /// <returns>Тот же список модулей после завершения загрузки entries.</returns>
    Task<IReadOnlyList<Module>> LoadSearchScopeEntriesAsync(IReadOnlyList<Module> modules);
    
    /// <summary>
    /// Дожидается загрузки списков модулей всех domain.
    /// </summary>
    /// <returns>Задача, завершающаяся после загрузки каталогов модулей для всех domain.</returns>
    Task EnsureDomainModuleListsLoadedAsync();
}