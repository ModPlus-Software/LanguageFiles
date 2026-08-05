using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow;

using System.Collections.ObjectModel;
using Core.Abstractions;
using Models;

/// <summary>
/// ViewModel панели навигации domain / module.
/// </summary>
public class NavBarVM
{
    private readonly IEditorWorkspace _workspace;
    private readonly IEditorSession _session;

    /// <summary>
    /// Доступные domain.
    /// </summary>
    public ObservableCollection<Domain> Domains => _session.Domains;

    /// <summary>
    /// Выбранный domain.
    /// </summary>
    public Domain SelectedDomain
    {
        get => _workspace.SelectedDomain;
        set => _workspace.SelectedDomain = value;
    }

    /// <summary>
    /// Выбранный module.
    /// </summary>
    public Module SelectedModule
    {
        get => _workspace.SelectedModule;
        set => _workspace.SelectedModule = value;
    }

    /// <summary>
    /// Создаёт NavBar.
    /// </summary>
    /// <param name="workspace">Рабочая область с текущим выбором.</param>
    /// <param name="session">Сессия данных с доменами.</param>
    public NavBarVM(IEditorWorkspace workspace, IEditorSession session)
    {
        _workspace = workspace;
        _session = session;
    }
}