namespace LangFilesEditor.UI.Windows.MainWindow;

using Core.Abstractions;
using Core.Application;
using Helpers;
using LangFilesEditor.Models;
using Services;
using Infrastructure.Docking;
using WorkSpace;
using ModPlusAPI.Mvvm;

/// <summary>
/// Корневой ViewModel главного окна. Делегирует расширениям <see cref="IExtensionHost"/>.
/// </summary>
public class MainWindowVM : ObservableObject
{
    private readonly EditorBootstrap _bootstrap;
    private readonly IDialogService _dialogService;

    // todo: странно, что vm знает о ширине view. Такого быть не должно.
    /// <summary>
    /// Создаёт shell редактора с панелями и подключёнными расширениями.
    /// </summary>
    /// <param name="toolBarDockPanelWidth">Ширина закреплённой панели инструментов.</param>
    /// <param name="bootstrap">todo: словно здесь этого вообще быть не должно. Странно оно.</param>
    public MainWindowVM(double toolBarDockPanelWidth, EditorBootstrap bootstrap)
    {
        _bootstrap = bootstrap;
        EditorThemeManager.Apply(_bootstrap.Settings.Current.Theme);
        _dialogService = new DialogService(
            _bootstrap.Store,
            _bootstrap.Workspace,
            _bootstrap.ExtensionHost,
            _bootstrap.Diagnostics,
            _bootstrap.Repository,
            _bootstrap.Settings);
        // core-компоненты shell'а получают workspace (выбор, вкладки, режимы) и сессию данных
        // напрямую от bootstrap, а не через узкий IExtensionHost.Session, который предназначен
        // только для расширений.
        ToolBarVM = new ToolBarVM(
            _dialogService,
            _bootstrap.Workspace,
            _bootstrap.Store,
            () => RequestCloseEvent?.Invoke(),
            ToolbarCommands);
        NavBarVM = new NavBarVM(_bootstrap.Workspace, _bootstrap.Store);
        StatusBarVM = new StatusBarVM(
            _bootstrap.Workspace,
            _bootstrap.OperationTracker,
            _bootstrap.Diagnostics);
        SearchBarVM = new SearchBarVM(_bootstrap.Workspace, _bootstrap.Store, _bootstrap.SearchEngine, Domains);
        WorkSpaceVM = new WorkSpaceVM(_bootstrap.Workspace);
        // todo: локализация
        ToolPanel = new DockPanelState(
            "Tools",
            DockSide.Right,
            defaultWidth: toolBarDockPanelWidth,
            chromePlacement: DockChromePlacement.Right,
            minDockedSpan: toolBarDockPanelWidth,
            maxDockedSpan: toolBarDockPanelWidth);
    }
    
    /// <summary>
    /// Панель навигации (domain / module).
    /// </summary>
    /// todo: локализация
    public DockPanelState NavPanel { get; } = new("Navigation", DockSide.Left, defaultWidth: 300);
    
    /// <summary>
    /// Панель инструментов.
    /// </summary>
    public DockPanelState ToolPanel { get; }
    
    /// <summary>
    /// Панель поиска.
    /// </summary>
    public DockPanelState SearchPanel { get; } = new("Search", DockSide.Top, defaultHeight: 96);
    
    /// <summary>
    /// Панель атрибутов module.
    /// </summary>
    public DockPanelState AttributesPanel { get; } = new("Attributes", DockSide.Bottom, defaultHeight: 160);
    
    /// <summary>
    /// Запрос закрытия окна без сохранения (инициируется кнопкой toolbar, обрабатывается <see cref="MainWindow"/>).
    /// </summary>
    public event Action RequestCloseEvent;
    
    /// <summary>
    /// Toolbar ViewModel.
    /// </summary>
    public ToolBarVM ToolBarVM { get; }
    
    /// <summary>
    /// NavBar ViewModel.
    /// </summary>
    public NavBarVM NavBarVM { get; }
    
    /// <summary>
    /// StatusBar ViewModel.
    /// </summary>
    public StatusBarVM StatusBarVM { get; }
    
    /// <summary>
    /// SearchBar ViewModel.
    /// </summary>
    public SearchBarVM SearchBarVM { get; }
    
    /// <summary>
    /// Workspace ViewModel.
    /// </summary>
    public WorkSpaceVM WorkSpaceVM { get; }
    
    /// <summary>
    /// Меню видимости панелей.
    /// </summary>
    public WindowPanelsMenuVM WindowPanelsMenu { get; } = new();
    
    /// <inheritdoc />
    public IExtensionEditorSession Session => _bootstrap.ExtensionHost.Session;
    
    /// <inheritdoc />
    public IEditorCommands Commands => _bootstrap.Store;
    
    /// <inheritdoc />
    public IModuleEditor Modules => _bootstrap.ExtensionHost.Modules;
    
    /// <inheritdoc />
    public IBackgroundOperationService Operations => _bootstrap.ExtensionHost.Operations;
    
    /// <inheritdoc />
    public IDiagnosticsPublisher Diagnostics => _bootstrap.ExtensionHost.Diagnostics;
    
    /// <inheritdoc />
    public System.Collections.ObjectModel.ObservableCollection<Domain> Domains => _bootstrap.ExtensionHost.Domains;
    
    /// <inheritdoc />
    /// todo: к Store напрямую так нельзя обращаться.
    public bool SaveChanges() => _bootstrap.Store.Save();
    
    /// <inheritdoc />
    public void RegisterToolbarCommand(LangFilesEditorToolbarCommand command) =>
        _bootstrap.ExtensionHost.RegisterToolbarCommand(command);
    
    /// <inheritdoc />
    public IReadOnlyList<LangFilesEditorToolbarCommand> ToolbarCommands => _bootstrap.ExtensionHost.ToolbarCommands;
    
    /// <summary>
    /// Начальная загрузка каталога модулей всех domain и фоновое сканирование диагностики базы.
    /// </summary>
    /// <returns>Задача инициализации навигации и диагностики.</returns>
    public async Task InitializeNavigationAsync()
    {
        await _bootstrap.Store.EnsureDomainModuleListsLoadedAsync();
        if (_bootstrap.Settings.Current.RunStartupDiagnosticsScan)
        {
            await _bootstrap.Diagnostics.RunStartupScanAsync(_bootstrap.Repository, _bootstrap.Store.Languages);
        }
    }
    
    /// <summary>
    /// Сохраняет изменения при обычном закрытии окна.
    /// </summary>
    /// <returns><see langword="true"/>, если сохранение выполнено; <see langword="false"/> при ошибках валидации.</returns>
    public bool TrySaveOnExit()
    {
        if (SaveChanges())
        {
            return true;
        }
        
        // todo: локализация
        _dialogService.ShowMessageWindow(
            "Есть некорректные данные — изменения не сохранены." +
            " Исправьте ошибки или закройте без сохранения.");
        return false;
    }
}