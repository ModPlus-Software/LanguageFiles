namespace LangFilesEditor.Core.Application;

using System.Collections.ObjectModel;
using Models;
using Services;

/// <summary>
/// Владелец режимов отображения результатов поиска и диагностики: какой режим активен,
/// какие модули он показывает и в каком порядке, а также сброс фильтров модулей при выходе.
/// </summary>
internal sealed class WorkspaceResultsViewCoordinator
{
    private readonly ObservableCollection<Domain> _domains;
    private readonly ObservableCollection<Module> _openModules;
    private IReadOnlyList<Module> _searchResultModules = [];
    private IReadOnlyList<Module> _diagnosticResultModules = [];

    /// <summary>
    /// Создаёт координатор над коллекциями доменов и открытых модулей сессии.
    /// </summary>
    /// <param name="domains">Домены редактора (для сброса поисковых/диагностических фильтров модулей).</param>
    /// <param name="openModules">Открытые вкладки модулей (запасной список отображения и порядок сортировки).</param>
    public WorkspaceResultsViewCoordinator(ObservableCollection<Domain> domains, ObservableCollection<Module> openModules)
    {
        _domains = domains;
        _openModules = openModules;
    }

    /// <summary>
    /// Активен ли режим отображения результатов поиска вместо открытых вкладок.
    /// </summary>
    public bool IsSearchResultsView { get; private set; }

    /// <summary>
    /// Активен ли режим отображения результатов диагностики вместо открытых вкладок.
    /// </summary>
    public bool IsDiagnosticResultsView { get; private set; }

    /// <summary>
    /// Активная категория фильтра диагностики рабочей области.
    /// </summary>
    public DiagnosticSeverity? ActiveDiagnosticFilter { get; private set; }

    /// <summary>
    /// Модули для отображения в рабочей области с учётом текущего режима (диагностика/поиск/открытые вкладки).
    /// </summary>
    public IReadOnlyList<Module> GetDisplayModules()
    {
        if (IsDiagnosticResultsView && _diagnosticResultModules.Count > 0)
        {
            return OrderModulesForDisplay(_diagnosticResultModules);
        }

        if (IsSearchResultsView && _searchResultModules.Count > 0)
        {
            return OrderModulesForDisplay(_searchResultModules);
        }

        return _openModules.ToList();
    }

    /// <summary>
    /// Устанавливает активную категорию фильтра диагностики.
    /// </summary>
    public void SetActiveDiagnosticFilter(DiagnosticSeverity? severity) => ActiveDiagnosticFilter = severity;

    /// <summary>
    /// Включает или выключает режим результатов диагностики для указанных модулей.
    /// </summary>
    public void SetDiagnosticResultsView(bool active, IReadOnlyList<Module> modules)
    {
        modules ??= [];
        var newActive = active && modules.Count > 0;

        if (newActive)
        {
            ExitSearchResultsView();
        }

        _diagnosticResultModules = modules;
        if (!newActive)
        {
            ClearModuleSearchFilters();
            SetActiveDiagnosticFilter(null);
            ClearPartialDiagnosticLoads();
        }

        IsDiagnosticResultsView = newActive;
    }

    /// <summary>
    /// Включает или выключает режим результатов поиска для указанных модулей.
    /// </summary>
    public void SetSearchResultsView(bool active, IReadOnlyList<Module> modules)
    {
        modules ??= [];
        var newActive = active && modules.Count > 0;

        if (newActive)
        {
            ExitDiagnosticResultsView();
        }

        _searchResultModules = modules;
        if (!newActive)
        {
            ClearModuleSearchFilters();
            SetActiveDiagnosticFilter(null);
        }

        IsSearchResultsView = newActive;
    }

    /// <summary>
    /// Выходит из режима результатов поиска (если активен) и сбрасывает поисковые фильтры модулей.
    /// </summary>
    public void ExitSearchResultsView()
    {
        _searchResultModules = [];
        ClearModuleSearchFilters();
        IsSearchResultsView = false;
    }

    /// <summary>
    /// Выходит из режима результатов диагностики (если активен) и сбрасывает связанные фильтры.
    /// </summary>
    public void ExitDiagnosticResultsView()
    {
        _diagnosticResultModules = [];

        if (!IsDiagnosticResultsView)
        {
            return;
        }

        ClearPartialDiagnosticLoads();
        SetActiveDiagnosticFilter(null);
        ClearModuleSearchFilters();
        IsDiagnosticResultsView = false;
    }

    /// <summary>
    /// Сбрасывает пометку частичной загрузки диагностики у всех модулей всех доменов.
    /// </summary>
    public void ClearPartialDiagnosticLoads()
    {
        foreach (var domain in _domains)
        {
            foreach (var module in domain.Modules ?? [])
            {
                module.ClearPartialDiagnosticLoad();
            }
        }
    }

    private void ClearModuleSearchFilters()
    {
        foreach (var domain in _domains)
        {
            foreach (var module in domain.Modules ?? [])
            {
                if (module.SearchString != string.Empty)
                {
                    module.SearchString = string.Empty;
                }

                if (module.DiagnosticFilter.HasValue)
                {
                    module.DiagnosticFilter = null;
                }
            }
        }
    }

    private IReadOnlyList<Module> OrderModulesForDisplay(IReadOnlyList<Module> modules)
    {
        var result = new List<Module>();
        var set = modules.ToHashSet();
        foreach (var open in _openModules)
        {
            if (set.Contains(open))
            {
                result.Add(open);
            }
        }

        foreach (var module in modules)
        {
            if (!result.Contains(module))
            {
                result.Add(module);
            }
        }

        return result;
    }
}