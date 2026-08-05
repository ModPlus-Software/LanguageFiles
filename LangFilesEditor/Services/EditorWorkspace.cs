namespace LangFilesEditor.Services;

using System.Collections.ObjectModel;
using Core.Abstractions;
using Core.Application;
using Models;
using ModPlusAPI.Mvvm;

/// <summary>
/// Презентационное состояние рабочей области поверх <see cref="Store"/>: текущий выбор,
/// открытые вкладки и режимы отображения результатов поиска и диагностики. Собственного
/// состояния не хранит — делегирует его координаторам и транслирует изменения в binding.
/// </summary>
public sealed class EditorWorkspace : ObservableObject, IEditorWorkspace
{
    private readonly Store _store;
    private readonly SearchEngine _searchEngine;
    private readonly WorkspaceResultsViewCoordinator _resultsView;
    private readonly EditorSelectionCoordinator _selection;

    /// <summary>
    /// Создаёт workspace над указанной сессией.
    /// </summary>
    /// <param name="store">Сессия данных редактора (домены, загрузка, сохранение).</param>
    /// <param name="searchEngine">Единый движок поиска/фильтрации (одна инстанция на приложение).</param>
    public EditorWorkspace(Store store, SearchEngine searchEngine)
    {
        _store = store;
        _searchEngine = searchEngine;
        _selection = new EditorSelectionCoordinator(store.DomainLoads);
        _resultsView = new WorkspaceResultsViewCoordinator(store.Domains, OpenModules);
    }

    /// <inheritdoc />
    public ObservableCollection<Module> OpenModules { get; } = [];

    /// <summary>
    /// Активен ли режим отображения результатов поиска вместо открытых вкладок.
    /// Владелец состояния — <see cref="WorkspaceResultsViewCoordinator"/>; здесь только транслируется в binding.
    /// </summary>
    /// <inheritdoc />
    public bool IsSearchResultsView => _resultsView.IsSearchResultsView;

    /// <summary>
    /// Активен ли режим отображения результатов диагностики вместо открытых вкладок.
    /// Владелец состояния — <see cref="WorkspaceResultsViewCoordinator"/>; здесь только транслируется в binding.
    /// </summary>
    /// <inheritdoc />
    public bool IsDiagnosticResultsView => _resultsView.IsDiagnosticResultsView;

    /// <inheritdoc />
    public DiagnosticSeverity? ActiveDiagnosticFilter => _resultsView.ActiveDiagnosticFilter;

    /// <inheritdoc />
    public IReadOnlyList<Module> DisplayModules => _resultsView.GetDisplayModules();

    /// <summary>
    /// Владелец состояния и правил согласования выбора — <see cref="EditorSelectionCoordinator"/>;
    /// здесь значение только транслируется в binding.
    /// </summary>
    /// <inheritdoc />
    public Domain SelectedDomain
    {
        get => _selection.SelectedDomain;
        set => RunSelectionChange(() => _selection.SelectDomain(value));
    }

    /// <summary>
    /// Владелец состояния и правил согласования выбора — <see cref="EditorSelectionCoordinator"/>;
    /// workspace добавляет к выбору свои side-эффекты: выход из результатных режимов,
    /// открытие вкладки и полную загрузку entries.
    /// </summary>
    /// <inheritdoc />
    public Module SelectedModule
    {
        get => _selection.SelectedModule;
        set
        {
            RunSelectionChange(() => _selection.SelectModule(value));
            if (value == null)
            {
                return;
            }

            // Выходить из результатных режимов (и перестраивать workspace) есть смысл только когда
            // один из них активен; иначе обычный выбор модуля перестраивал бы весь грид с нуля.
            if (_resultsView.IsSearchResultsView || _resultsView.IsDiagnosticResultsView)
            {
                RunResultsViewChange(() =>
                {
                    _resultsView.ExitSearchResultsView();
                    _resultsView.ExitDiagnosticResultsView();
                });
            }

            AddToOpenModules(value);
            EnsureModuleFullyLoaded(value);
        }
    }

    /// <inheritdoc />
    public TranslationEntry SelectedTranslationEntry
    {
        get => _selection.SelectedTranslationEntry;
        set => RunSelectionChange(() => _selection.SelectTranslationEntry(value));
    }

    /// <inheritdoc />
    public void SetActiveDiagnosticFilter(DiagnosticSeverity? severity)
    {
        if (_resultsView.ActiveDiagnosticFilter == severity)
        {
            return;
        }

        _resultsView.SetActiveDiagnosticFilter(severity);
        OnPropertyChanged(nameof(ActiveDiagnosticFilter));
    }

    /// <inheritdoc />
    public void SetDiagnosticResultsView(bool active, IReadOnlyList<Module> modules) =>
        RunResultsViewChange(() => _resultsView.SetDiagnosticResultsView(active, modules));

    /// <inheritdoc />
    public void SetSearchResultsView(bool active, IReadOnlyList<Module> modules) =>
        RunResultsViewChange(() => _resultsView.SetSearchResultsView(active, modules));

    /// <summary>
    /// Выбор модуля в режиме результатов диагностики: без выхода из режима,
    /// без открытия вкладки и без запуска полной загрузки entries.
    /// </summary>
    /// <inheritdoc />
    public void SelectModuleDuringDiagnostic(Module module) =>
        RunSelectionChange(() => _selection.SelectModule(module));

    /// <summary>
    /// Выбор модуля в режиме результатов поиска: без выхода из режима, но с открытием
    /// вкладки и полной загрузкой entries — модуль остаётся открытым после выхода из поиска.
    /// </summary>
    /// <inheritdoc />
    public void SelectModuleDuringSearch(Module module)
    {
        var changed = false;
        RunSelectionChange(() => changed = _selection.SelectModule(module));
        if (!changed)
        {
            return;
        }

        AddToOpenModules(module);
        EnsureModuleFullyLoaded(module);
    }

    /// <inheritdoc />
    public async Task ShowModuleDiagnosticAsync(Module module, DiagnosticSeverity severity)
    {
        if (module == null)
        {
            return;
        }

        RunResultsViewChange(() =>
        {
            _resultsView.ExitSearchResultsView();
            _resultsView.ClearPartialDiagnosticLoads();
            _resultsView.SetActiveDiagnosticFilter(severity);
        });
        _searchEngine.ApplyDiagnosticFilter(_store.Domains, [module], severity);
        SetDiagnosticResultsView(true, [module]);

        if (!await _store.DiagnosticLoads.LoadModuleAsync(module, severity, OpenModules))
        {
            SetDiagnosticResultsView(false, []);
            SetActiveDiagnosticFilter(null);
            return;
        }

        SelectModuleDuringDiagnostic(module);
    }

    /// <inheritdoc />
    public async Task ShowDiagnosticFilterAsync(DiagnosticSeverity severity, IReadOnlyList<Module> modules)
    {
        modules ??= [];
        if (modules.Count == 0)
        {
            return;
        }

        RunResultsViewChange(() =>
        {
            _resultsView.ExitSearchResultsView();
            _resultsView.ClearPartialDiagnosticLoads();
            _resultsView.SetActiveDiagnosticFilter(severity);
        });
        _searchEngine.ApplyDiagnosticFilter(_store.Domains, modules, severity);
        SetDiagnosticResultsView(true, modules);

        var loaded = await _store.DiagnosticLoads.LoadScopeAsync(modules, severity, OpenModules);
        if (loaded.Count == 0)
        {
            SetDiagnosticResultsView(false, []);
            SetActiveDiagnosticFilter(null);
            return;
        }

        if (loaded.Count != modules.Count)
        {
            SetDiagnosticResultsView(true, loaded);
        }
    }

    /// <inheritdoc />
    public void CloseModule(Module module)
    {
        if (module == null || !OpenModules.Contains(module))
        {
            return;
        }

        _store.EntryLoads.Cancel(module);
        var index = OpenModules.IndexOf(module);
        OpenModules.Remove(module);

        if (!ReferenceEquals(_selection.SelectedModule, module))
        {
            return;
        }

        var next = OpenModules.Count > 0 ? OpenModules[Math.Min(index, OpenModules.Count - 1)] : null;
        RunSelectionChange(() => _selection.SelectModule(next));

        if (next != null)
        {
            EnsureModuleFullyLoaded(next);
        }
    }

    /// <inheritdoc />
    public void BeginLoadModuleEntries(Module module)
    {
        if (module == null || !OpenModules.Contains(module))
        {
            return;
        }

        _ = _store.EntryLoads.LoadIfEmptyAsync(
            module,
            module.Group,
            reportToStatusBar: true,
            () => OpenModules.Contains(module));
    }

    /// <inheritdoc />
    public bool IsModuleEntriesLoading(Module module) => _store.EntryLoads.IsLoading(module);

    /// <summary>
    /// Выполняет мутацию состояния <see cref="EditorSelectionCoordinator"/> и транслирует изменившиеся
    /// значения выбора в <see cref="ObservableObject.PropertyChanged"/>. Единая точка нотификаций
    /// для всех операций выбора — сам workspace состояние выбора не хранит.
    /// </summary>
    /// <param name="mutate">Действие, вызывающее один или несколько методов координатора выбора.</param>
    private void RunSelectionChange(Action mutate)
    {
        var module = _selection.SelectedModule;
        var domain = _selection.SelectedDomain;
        var entry = _selection.SelectedTranslationEntry;

        mutate();

        if (!ReferenceEquals(module, _selection.SelectedModule))
        {
            OnPropertyChanged(nameof(SelectedModule));
        }

        if (!ReferenceEquals(domain, _selection.SelectedDomain))
        {
            OnPropertyChanged(nameof(SelectedDomain));
        }

        if (!ReferenceEquals(entry, _selection.SelectedTranslationEntry))
        {
            OnPropertyChanged(nameof(SelectedTranslationEntry));
        }
    }

    /// <summary>
    /// Выполняет мутацию состояния <see cref="WorkspaceResultsViewCoordinator"/> и транслирует изменившиеся
    /// значения в <see cref="ObservableObject.PropertyChanged"/>. Единая точка нотификаций для всех операций
    /// над результатными отображениями (поиск/диагностика).
    /// </summary>
    /// <param name="mutate">Действие, вызывающее один или несколько методов координатора.</param>
    private void RunResultsViewChange(Action mutate)
    {
        var wasSearch = _resultsView.IsSearchResultsView;
        var wasDiagnostic = _resultsView.IsDiagnosticResultsView;
        var wasFilter = _resultsView.ActiveDiagnosticFilter;

        mutate();

        if (wasSearch != _resultsView.IsSearchResultsView)
        {
            OnPropertyChanged(nameof(IsSearchResultsView));
        }

        if (wasDiagnostic != _resultsView.IsDiagnosticResultsView)
        {
            OnPropertyChanged(nameof(IsDiagnosticResultsView));
        }

        if (wasFilter != _resultsView.ActiveDiagnosticFilter)
        {
            OnPropertyChanged(nameof(ActiveDiagnosticFilter));
        }

        // DisplayModules меняется только при входе/выходе из результатных режимов или при смене
        // их содержимого. В обычном режиме вкладок список равен OpenModules, изменения которого
        // ModuleViewVM обрабатывает точечно через CollectionChanged — полная перестройка не нужна.
        var wasResultsView = wasSearch || wasDiagnostic;
        var isResultsView = _resultsView.IsSearchResultsView || _resultsView.IsDiagnosticResultsView;
        if (wasResultsView || isResultsView)
        {
            OnPropertyChanged(nameof(DisplayModules));
        }
    }

    private void EnsureModuleFullyLoaded(Module module)
    {
        if (module == null || !OpenModules.Contains(module))
        {
            return;
        }

        if (module.DiagnosticFilter.HasValue)
        {
            module.DiagnosticFilter = null;
        }

        // Пока идёт загрузка entries, ItemsLoadState ещё не Full — сброс в этот момент стёр бы
        // уже накопленные строки прямо посреди загрузки. Дождёмся её завершения.
        if (_store.EntryLoads.IsLoading(module))
        {
            return;
        }

        if (module.ItemsLoadState != ModuleItemsLoadState.Full)
        {
            module.ResetIncompleteLoad();
            BeginLoadModuleEntries(module);
        }
    }

    private void AddToOpenModules(Module module)
    {
        if (OpenModules.Contains(module))
        {
            return;
        }

        OpenModules.Add(module);
    }
}