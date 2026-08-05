using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using Core.Abstractions;
using Services;
using ModPlusAPI.Mvvm;

/// <summary>
/// ViewModel панели поиска по записям локализации.
/// </summary>
public class SearchBarVM : ObservableObject
{
    private const int SearchDebounceMilliseconds = 400;
    private readonly IEditorWorkspace _workspace;
    private readonly IEditorSession _session;
    private readonly SearchEngine _searchEngine;
    private readonly ObservableCollection<Domain> _domains;
    private readonly DispatcherTimer _debounceTimer;
    private int _searchVersion;
    private bool _filterReapplyScheduled;
    private string _queryText = string.Empty;
    private bool _searchInCommon;
    private bool _searchInOpenModules;
    private bool _searchInAllModules;

    /// <summary>
    /// Создаёт ViewModel панели поиска с отложенным выполнением запроса.
    /// </summary>
    /// <param name="workspace">Рабочая область с выбором модуля и режимом результатов поиска.</param>
    /// <param name="session">Сессия данных для догрузки entries области поиска.</param>
    /// <param name="searchEngine">Движок фильтрации и поиска по модулям.</param>
    /// <param name="domains">Коллекция доменов для поиска по всем модулям.</param>
    public SearchBarVM(
        IEditorWorkspace workspace,
        IEditorSession session,
        SearchEngine searchEngine,
        ObservableCollection<Domain> domains)
    {
        _workspace = workspace;
        _session = session;
        _searchEngine = searchEngine;
        _domains = domains;
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(SearchDebounceMilliseconds)
        };

        _debounceTimer.Tick += async (_, _) =>
        {
            _debounceTimer.Stop();
            await ExecuteSearchAsync();
        };

        _workspace.PropertyChanged += OnWorkspacePropertyChanged;
    }

    /// <summary>
    /// Текст поискового запроса.
    /// </summary>
    public string QueryText
    {
        get => _queryText;
        set
        {
            if (_queryText == value)
            {
                return;
            }

            _queryText = value;
            OnPropertyChanged();
            ScheduleSearch();
        }
    }

    /// <summary>
    /// Искать в общем модуле (Common).
    /// </summary>
    public bool SearchInCommon
    {
        get => _searchInCommon;
        set
        {
            if (_searchInCommon == value)
            {
                return;
            }

            _searchInCommon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSearchInCommonEnabled));
            ScheduleSearch();
        }
    }

    /// <summary>
    /// Искать среди открытых модулей.
    /// </summary>
    public bool SearchInOpenModules
    {
        get => _searchInOpenModules;
        set
        {
            if (_searchInOpenModules == value)
            {
                return;
            }

            _searchInOpenModules = value;

            if (value)
            {
                SetSearchInCommonIncluded();
            }
            else if (!_searchInAllModules)
            {
                SearchInCommon = false;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSearchInCommonEnabled));
            OnPropertyChanged(nameof(IsSearchInOpenModulesEnabled));
            ScheduleSearch();
        }
    }

    /// <summary>
    /// Искать во всех модулях всех доменов.
    /// </summary>
    public bool SearchInAllModules
    {
        get => _searchInAllModules;
        set
        {
            if (_searchInAllModules == value)
            {
                return;
            }

            _searchInAllModules = value;
            if (value)
            {
                _searchInOpenModules = true;
                _searchInCommon = true;
                OnPropertyChanged(nameof(SearchInOpenModules));
                OnPropertyChanged(nameof(SearchInCommon));
            }
            else
            {
                _searchInOpenModules = false;
                _searchInCommon = false;
                OnPropertyChanged(nameof(SearchInOpenModules));
                OnPropertyChanged(nameof(SearchInCommon));
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSearchInCommonEnabled));
            OnPropertyChanged(nameof(IsSearchInOpenModulesEnabled));
            ScheduleSearch();
        }
    }

    /// <summary>
    /// Доступен ли флаг поиска в Common (не при открытых или всех модулях).
    /// </summary>
    public bool IsSearchInCommonEnabled => !SearchInOpenModules && !SearchInAllModules;

    /// <summary>
    /// Доступен ли флаг поиска в открытых модулях (не при поиске по всем).
    /// </summary>
    public bool IsSearchInOpenModulesEnabled => !SearchInAllModules;

    private void OnWorkspacePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            // Диагностика и текстовый поиск — взаимоисключающие режимы: включение фильтра
            // диагностики очищает поисковый запрос.
            case nameof(IEditorWorkspace.ActiveDiagnosticFilter)
                when _workspace.ActiveDiagnosticFilter != null && !string.IsNullOrEmpty(_queryText):
                _queryText = string.Empty;
                OnPropertyChanged(nameof(QueryText));
                break;

            // Открытие нового модуля (клик по узлу дерева или по вкладке) не должно сбрасывать
            // введённый запрос: фильтр переприменяется к новому составу области поиска.
            case nameof(IEditorWorkspace.SelectedModule):
                ScheduleFilterReapply();
                break;
        }
    }

    /// <summary>
    /// Планирует переприменение текущего запроса к области поиска.
    /// Выбор модуля выключает режим результатов поиска уже после уведомления о смене выбора,
    /// поэтому фактическое применение откладывается до конца текущей операции — к этому моменту
    /// режим результатов уже выключен и фильтр ложится на обычный список вкладок.
    /// </summary>
    private void ScheduleFilterReapply()
    {
        if (_filterReapplyScheduled || string.IsNullOrWhiteSpace(_queryText))
        {
            return;
        }

        _filterReapplyScheduled = true;
        _debounceTimer.Dispatcher.BeginInvoke(() => ReapplyFilterToCurrentScope(), DispatcherPriority.Background);
    }

    private void ReapplyFilterToCurrentScope()
    {
        _filterReapplyScheduled = false;

        // В результатных режимах состав отображения задаётся самим поиском/диагностикой —
        // там переприменять фильтр не нужно.
        if (_workspace.IsSearchResultsView
            || _workspace.IsDiagnosticResultsView
            || string.IsNullOrWhiteSpace(_queryText))
        {
            return;
        }

        _searchEngine.ApplySearch(
            _workspace.SelectedModule,
            _workspace.SelectedModule?.Group,
            _domains,
            _workspace.OpenModules,
            _queryText,
            CreateScope());
    }

    private void SetSearchInCommonIncluded()
    {
        if (_searchInCommon)
        {
            return;
        }

        _searchInCommon = true;
        OnPropertyChanged(nameof(SearchInCommon));
        OnPropertyChanged(nameof(IsSearchInCommonEnabled));
    }

    private void ScheduleSearch()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private SearchScopeOptions CreateScope() => new()
    {
        SearchInCommon = SearchInCommon,
        SearchInOpenModules = SearchInOpenModules,
        SearchInAllModules = SearchInAllModules
    };

    private async Task ExecuteSearchAsync()
    {
        var version = ++_searchVersion;
        var scope = CreateScope();

        var hasQuery = !string.IsNullOrWhiteSpace(_queryText);
        if (hasQuery)
        {
            _workspace.SetActiveDiagnosticFilter(null);
            _workspace.SetDiagnosticResultsView(false, []);
        }

        if (!hasQuery)
        {
            _searchEngine.ApplySearch(
                _workspace.SelectedModule,
                _workspace.SelectedModule?.Group,
                _domains,
                _workspace.OpenModules,
                string.Empty,
                scope);
            _workspace.SetSearchResultsView(false, []);
            return;
        }

        if (scope.SearchInAllModules)
        {
            await _session.EnsureDomainModuleListsLoadedAsync();
        }

        if (version != _searchVersion)
        {
            return;
        }

        var targets = _searchEngine.ResolveTargetModules(
            _workspace.SelectedModule,
            _workspace.SelectedModule?.Group,
            _domains,
            _workspace.OpenModules,
            scope);
        targets = await _session.LoadSearchScopeEntriesAsync(targets);

        if (version != _searchVersion)
        {
            return;
        }

        var modulesWithResults = _searchEngine.GetModulesWithResults(targets, _queryText);
        if (modulesWithResults.Count == 0)
        {
            _searchEngine.ApplySearch(
                _workspace.SelectedModule,
                _workspace.SelectedModule?.Group,
                _domains,
                _workspace.OpenModules,
                string.Empty,
                scope);
            _workspace.SetSearchResultsView(false, []);
            return;
        }

        _searchEngine.ApplySearch(
            _workspace.SelectedModule,
            _workspace.SelectedModule?.Group,
            _domains,
            _workspace.OpenModules,
            _queryText,
            scope);
        _workspace.SetSearchResultsView(true, modulesWithResults);
    }
}