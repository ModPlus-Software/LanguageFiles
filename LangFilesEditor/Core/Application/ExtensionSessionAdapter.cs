namespace LangFilesEditor.Core.Application;

using System.Collections.ObjectModel;
using System.ComponentModel;
using Abstractions;
using Models;

/// <summary>
/// Собирает узкий read-only срез <see cref="IExtensionEditorSession"/> для расширений из двух источников:
/// данные (языки, домены, занятость) — из сессии, выбор — из workspace. Расширениям без разницы,
/// кто за фасадом; core при этом не обязан держать всё в одном объекте.
/// </summary>
public sealed class ExtensionSessionAdapter : IExtensionEditorSession
{
    private readonly IEditorSession _session;
    private readonly IEditorWorkspace _workspace;
    
    /// <summary>
    /// Создаёт адаптер над сессией и workspace.
    /// </summary>
    /// <param name="session">Сессия данных редактора.</param>
    /// <param name="workspace">Состояние рабочей области с текущим выбором.</param>
    public ExtensionSessionAdapter(IEditorSession session, IEditorWorkspace workspace)
    {
        _session = session;
        _workspace = workspace;
        _session.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(IEditorSession.Domains)
                or nameof(IEditorSession.IsOperationInProgress))
            {
                PropertyChanged?.Invoke(this, e);
            }
        };
        _workspace.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(IEditorWorkspace.SelectedDomain)
                or nameof(IEditorWorkspace.SelectedModule)
                or nameof(IEditorWorkspace.SelectedTranslationEntry))
            {
                PropertyChanged?.Invoke(this, e);
            }
        };
    }
    
    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;
    
    /// <inheritdoc />
    public IReadOnlyList<string> Languages => _session.Languages;
    
    /// <inheritdoc />
    public ObservableCollection<Domain> Domains => _session.Domains;
    
    /// <inheritdoc />
    public Domain SelectedDomain => _workspace.SelectedDomain;
    
    /// <inheritdoc />
    public Module SelectedModule => _workspace.SelectedModule;
    
    /// <inheritdoc />
    public TranslationEntry SelectedTranslationEntry => _workspace.SelectedTranslationEntry;
    
    /// <inheritdoc />
    public bool IsOperationInProgress => _session.IsOperationInProgress;
}