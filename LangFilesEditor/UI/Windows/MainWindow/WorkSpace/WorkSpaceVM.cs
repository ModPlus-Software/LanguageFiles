using LangFilesEditor.Services;

namespace LangFilesEditor.UI.Windows.MainWindow.WorkSpace;

using System.ComponentModel;
using Core.Abstractions;
using ModPlusAPI.Mvvm;

/// <summary>
/// Корневой ViewModel рабочей области.
/// </summary>
public class WorkSpaceVM : ObservableObject
{
    private readonly IEditorWorkspace _workspace;

    /// <summary>
    /// Создаёт workspace с module grid и вкладками.
    /// </summary>
    /// <param name="workspace">Состояние рабочей области с открытыми модулями и режимами отображения.</param>
    public WorkSpaceVM(IEditorWorkspace workspace)
    {
        _workspace = workspace;
        _workspace.PropertyChanged += OnWorkspacePropertyChanged;
        ModuleViewVM = new ModuleViewVM(workspace);
        ModulesBarVM = new ModulesBarVM(workspace);
    }

    /// <summary>
    /// Грид entries и заголовков modules.
    /// </summary>
    public ModuleViewVM ModuleViewVM { get; }

    /// <summary>
    /// Вкладки открытых modules.
    /// </summary>
    public ModulesBarVM ModulesBarVM { get; }

    /// <summary>
    /// Панель вкладок скрыта в режиме просмотра диагностики.
    /// </summary>
    public bool IsModulesBarVisible => !_workspace.IsDiagnosticResultsView;

    private void OnWorkspacePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IEditorWorkspace.IsDiagnosticResultsView))
        {
            OnPropertyChanged(nameof(IsModulesBarVisible));
        }
    }
}