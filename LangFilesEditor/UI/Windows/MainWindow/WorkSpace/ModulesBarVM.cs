using LangFilesEditor.Models;
using LangFilesEditor.Services;

namespace LangFilesEditor.UI.Windows.MainWindow.WorkSpace;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Core.Abstractions;
using Models;
using ModPlusAPI.Mvvm;

/// <summary>
/// ViewModel вкладок открытых modules.
/// </summary>
public class ModulesBarVM : ObservableObject
{
    private readonly IEditorWorkspace _workspace;

    /// <summary>
    /// Создаёт панель вкладок modules.
    /// </summary>
    /// <param name="workspace">Состояние рабочей области с открытыми модулями и выбором.</param>
    public ModulesBarVM(IEditorWorkspace workspace)
    {
        _workspace = workspace;
        _workspace.PropertyChanged += OnWorkspacePropertyChanged;
        SelectModuleCommand = new RelayCommand<Module>(m =>
        {
            if (m != null)
            {
                _workspace.SelectedModule = m;
            }
        });
        CloseModuleCommand = new RelayCommand<Module>(m =>
        {
            if (m != null)
            {
                _workspace.CloseModule(m);
            }
        });
    }

    /// <summary>
    /// Открытые modules.
    /// </summary>
    public ObservableCollection<Module> OpenModules => _workspace.OpenModules;

    /// <summary>
    /// Выбранный module.
    /// </summary>
    public Module SelectedModule => _workspace.SelectedModule;

    /// <summary>
    /// Выбор module по клику на вкладке.
    /// </summary>
    public ICommand SelectModuleCommand { get; }

    /// <summary>
    /// Закрытие вкладки module.
    /// </summary>
    public ICommand CloseModuleCommand { get; }

    private void OnWorkspacePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IEditorWorkspace.SelectedModule))
        {
            OnPropertyChanged(nameof(SelectedModule));
        }
    }
}