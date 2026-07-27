namespace LangFilesEditor.Core.Abstractions;

using System.Collections.ObjectModel;
using Models;

/// <summary>
/// Единая точка входа расширений в приложение перевода.
/// </summary>
public interface IExtensionHost
{
    /// <summary>
    /// Доступ к активной сессии работающего приложения.
    /// </summary>
    IExtensionEditorSession Session { get; }
    
    /// <summary>
    /// Команды редактора.
    /// </summary>
    IEditorCommands Commands { get; }
    
    /// <summary>
    /// Интерфейс изменения модулей и единиц перевода.
    /// </summary>
    IModuleEditor Modules { get; }
    
    /// <summary>
    /// Фоновые операции.
    /// </summary>
    IBackgroundOperationService Operations { get; }
    
    /// <summary>
    /// Публикация статусов.
    /// </summary>
    IDiagnosticsPublisher Diagnostics { get; }
    
    /// <summary>
    /// Домены.
    /// </summary>
    ObservableCollection<Domain> Domains { get; }
    
    /// <summary>
    /// Команды, встраиваемые в toolbar расширениями.
    /// </summary>
    IReadOnlyList<LangFilesEditorToolbarCommand> ToolbarCommands { get; }
}