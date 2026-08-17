using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow.WorkSpace;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Core.Abstractions;
using Models;
using Helpers;
using Services;
using Utils;
using ModPlusAPI.Mvvm;

/// <summary>
/// Строки workspace: заголовок модуля + проекция <see cref="Module.Items"/> в грид.
/// При загрузке — по одной строке через <see cref="Module.Items"/> CollectionChanged.
/// </summary>
public class ModuleViewVM : ObservableObject
{
    private readonly IEditorWorkspace _store;
    private readonly Dictionary<Module, CancellationTokenSource> _populateCts = new();
    private readonly HashSet<Module> _subscribedModules = new();
    private int _rebuildVersion;
    private bool _suppressScrollToModule;
    private Module _pendingScrollModule;

    /// <summary>
    /// Создаёт ViewModel грида workspace и подписывается на рабочую область.
    /// </summary>
    /// <param name="workspace">Состояние рабочей области с открытыми модулями и выбором.</param>
    public ModuleViewVM(IEditorWorkspace workspace)
    {
        _store = workspace;
        _store.PropertyChanged += OnStorePropertyChanged;
        _store.OpenModules.CollectionChanged += OnOpenModulesChanged;
        RebuildAllRows();
    }

    /// <summary>
    /// Строки грида: заголовки модулей и записи перевода.
    /// </summary>
    public ObservableCollection<WorkspaceGridRow> Rows { get; } = [];

    /// <summary>
    /// Выбранный в сессии модуль.
    /// </summary>
    public Module SelectedModule => _store.SelectedModule;

    /// <summary>
    /// Запрос прокрутки грида к заголовку указанного модуля.
    /// </summary>
    public event Action<Module> ScrollToModuleRequested;

    /// <summary>
    /// Обрабатывает смену выбранной строки в гриде и синхронизирует сессию.
    /// </summary>
    /// <param name="selectedRow">Выбранная строка (<see cref="TranslationEntryGridRow"/> или <see cref="ModuleHeaderGridRow"/>).</param>
    public void OnGridSelectionChanged(object selectedRow)
    {
        _suppressScrollToModule = true;
        try
        {
            switch (selectedRow)
            {
                case TranslationEntryGridRow entryRow:
                    SelectModuleFromGrid(entryRow.Module);
                    if (!ReferenceEquals(_store.SelectedTranslationEntry, entryRow.Entry))
                    {
                        _store.SelectedTranslationEntry = entryRow.Entry;
                    }
                    break;
                case ModuleHeaderGridRow headerRow:
                    SelectModuleFromGrid(headerRow.Module);
                    _store.SelectedTranslationEntry = null;
                    break;
            }
        }
        finally
        {
            _suppressScrollToModule = false;
        }
    }

    /// <summary>
    /// Находит строку-заголовок указанного модуля в коллекции строк.
    /// </summary>
    /// <param name="module">Модуль, заголовок которого ищется.</param>
    /// <returns>Строка заголовка или <see langword="null"/>, если модуль не представлен в гриде.</returns>
    public ModuleHeaderGridRow FindModuleHeaderRow(Module module)
    {
        var index = WorkspaceGridLayoutHelper.FindHeaderIndex(Rows, module);
        return index >= 0 ? (ModuleHeaderGridRow)Rows[index] : null;
    }

    private void OnOpenModulesChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (_store.IsSearchResultsView || _store.IsDiagnosticResultsView)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    var index = e.NewStartingIndex >= 0
                        ? WorkspaceGridLayoutHelper.HeaderInsertIndexForModule(_store.OpenModules, Rows, e.NewStartingIndex)
                        : Rows.Count;
                    foreach (Module module in e.NewItems!)
                    {
                        AttachModule(module, index);
                        index = WorkspaceGridLayoutHelper.GetInsertIndexAfterModule(Rows, module);
                    }
                    break;
                }
            case NotifyCollectionChangedAction.Remove:
                foreach (Module module in e.OldItems!)
                {
                    DetachModule(module);
                }
                break;
            default:
                RebuildAllRows();
                break;
        }
    }

    private void SelectModuleFromGrid(Module module)
    {
        if (module == null)
        {
            return;
        }

        if (_store.IsSearchResultsView)
        {
            _store.SelectModuleDuringSearch(module);
        }
        else if (_store.IsDiagnosticResultsView)
        {
            _store.SelectModuleDuringDiagnostic(module);
        }
        else if (!ReferenceEquals(_store.SelectedModule, module))
        {
            _store.SelectedModule = module;
        }
    }

    private void OnStorePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IEditorWorkspace.IsSearchResultsView)
            or nameof(IEditorWorkspace.IsDiagnosticResultsView)
            or nameof(IEditorWorkspace.DisplayModules))
        {
            RebuildAllRows();
        }

        if (e.PropertyName is nameof(IEditorWorkspace.SelectedModule))
        {
            OnPropertyChanged(nameof(SelectedModule));
            RequestScrollToSelectedModule();
            EnsureEntryRowsForSelectedModule();
        }
    }

    private void EnsureEntryRowsForSelectedModule()
    {
        var module = _store.SelectedModule;
        if (module == null || module.Items.Count == 0)
        {
            return;
        }

        if (WorkspaceGridLayoutHelper.FindHeaderIndex(Rows, module) < 0)
        {
            return;
        }

        if (WorkspaceGridLayoutHelper.HasEntryRows(Rows, module))
        {
            return;
        }

        StartPopulateVisibleEntries(module);
    }

    /// <summary>
    /// Запрашивает прокрутку к выбранному модулю. Заголовок модуля может ещё отсутствовать в гриде:
    /// при открытии нового узла уведомление о смене выбора приходит раньше, чем модуль попадает
    /// в <see cref="IEditorWorkspace.OpenModules"/> и в сетку. Поэтому запрос запоминается и
    /// повторяется, когда строка заголовка появится и когда допишутся строки модуля.
    /// </summary>
    private void RequestScrollToSelectedModule()
    {
        if (_suppressScrollToModule)
        {
            return;
        }

        var module = _store.SelectedModule;
        if (module == null)
        {
            return;
        }

        _pendingScrollModule = module;
        ScrollToModuleRequested?.Invoke(module);
    }

    /// <summary>
    /// Повторяет отложенный запрос прокрутки, если он относится к указанному модулю.
    /// </summary>
    /// <param name="module">Модуль, для которого изменился состав строк.</param>
    /// <param name="complete"><see langword="true"/> — запрос выполнен и больше не повторяется.</param>
    private void RepeatPendingScroll(Module module, bool complete)
    {
        if (!ReferenceEquals(_pendingScrollModule, module))
        {
            return;
        }

        if (complete)
        {
            _pendingScrollModule = null;
        }

        ScrollToModuleRequested?.Invoke(module);
    }

    private void RebuildAllRows()
    {
        CancelAllPopulates();
        UnsubscribeAllModules();
        Rows.Clear();
        var version = ++_rebuildVersion;
        _ = RebuildAllRowsAsync(version);
    }

    private async Task RebuildAllRowsAsync(int version)
    {
        foreach (var module in _store.DisplayModules.ToList())
        {
            if (version != _rebuildVersion)
            {
                return;
            }

            await AttachModuleAsync(module, Rows.Count, awaitPopulate: true);
            await Application.Current.Dispatcher.YieldAsync();
        }
    }

    private void AttachModule(Module module, int headerInsertIndex) => _ = AttachModuleAsync(module, headerInsertIndex);

    private async Task AttachModuleAsync(Module module, int headerInsertIndex, bool awaitPopulate = false)
    {
        if (!_store.DisplayModules.Contains(module))
        {
            return;
        }

        CancelPopulate(module);
        SubscribeModule(module);

        if (WorkspaceGridLayoutHelper.FindHeaderIndex(Rows, module) < 0)
        {
            Rows.Insert(headerInsertIndex, new ModuleHeaderGridRow(module, IsAlternateModuleView));
            RepeatPendingScroll(module, complete: false);
        }

        if (_store.OpenModules.Contains(module) && module.ItemsLoadState != ModuleItemsLoadState.Full)
        {
            if (!_store.IsModuleEntriesLoading(module))
            {
                _store.BeginLoadModuleEntries(module);
            }
        }
        else if (module.Items.Count > 0)
        {
            if (!WorkspaceGridLayoutHelper.HasEntryRows(Rows, module) && !module.IsBulkItemsLoading)
            {
                var populate = RunPopulateVisibleEntriesAsync(module);
                if (awaitPopulate)
                {
                    await populate;
                }
            }
        }
        else if (!_store.IsModuleEntriesLoading(module) && _store.OpenModules.Contains(module))
        {
            _store.BeginLoadModuleEntries(module);
        }
    }

    private void DetachModule(Module module)
    {
        if (ReferenceEquals(_pendingScrollModule, module))
        {
            _pendingScrollModule = null;
        }

        CancelPopulate(module);
        UnsubscribeModule(module);
        for (var i = Rows.Count - 1; i >= 0; i--)
        {
            if (Rows[i].Module == module)
            {
                if (Rows[i] is TranslationEntryGridRow entryRow)
                {
                    entryRow.Detach();
                }

                Rows.RemoveAt(i);
            }
        }
    }

    private void StartPopulateVisibleEntries(Module module) => _ = RunPopulateVisibleEntriesAsync(module);

    private Task RunPopulateVisibleEntriesAsync(Module module)
    {
        CancelPopulate(module);
        var cts = new CancellationTokenSource();
        _populateCts[module] = cts;
        return PopulateVisibleEntriesAsync(module, cts);
    }

    private async Task PopulateVisibleEntriesAsync(Module module, CancellationTokenSource cts)
    {
        try
        {
            var yieldBudget = new UiYieldBudget(
                EditorSettingsStore.Instance.Current.InitialRowsPerFrame,
                EditorSettingsStore.Instance.Current.MaxRowsPerFrame);
            foreach (var entry in ModuleViewHelper.GetVisibleEntries(module))
            {
                cts.Token.ThrowIfCancellationRequested();
                TryInsertEntryRow(module, entry);

                if (!yieldBudget.RegisterRow())
                {
                    continue;
                }

                await Application.Current.Dispatcher.YieldAsync(DispatcherPriority.Background);
                yieldBudget.Reset();
            }
        }

        catch (OperationCanceledException)
        {
            // module closed or grid reset
        }
        finally
        {
            if (_populateCts.TryGetValue(module, out var current) && ReferenceEquals(current, cts))
            {
                _populateCts.Remove(module);
                cts.Dispose();

                // Запрос прокрутки гасится, только когда строки модуля действительно оказались
                // в сетке. Пустое наполнение бывает штатно: перед чтением с диска модуль сбрасывает
                // неполную загрузку (Items.Clear), и сетка перестраивается на нулевом составе —
                // погасив запрос здесь, мы бы оставили заголовок внизу таблицы навсегда.
                RepeatPendingScroll(
                    module,
                    complete: WorkspaceGridLayoutHelper.HasEntryRows(Rows, module) || module.EntryCount == 0);
            }
        }
    }

    private void CancelPopulate(Module module)
    {
        if (!_populateCts.Remove(module, out var cts))
        {
            return;
        }

        cts.Cancel();
        cts.Dispose();
    }

    private void CancelAllPopulates()
    {
        foreach (var module in _populateCts.Keys.ToList())
        {
            CancelPopulate(module);
        }
    }

    private void SubscribeModule(Module module)
    {
        if (!_subscribedModules.Add(module))
        {
            return;
        }

        module.Items.CollectionChanged += OnModuleItemsChanged;
        module.PropertyChanged += OnModulePropertyChanged;
        module.BulkItemsLoadCompleted += OnModuleBulkItemsLoadCompleted;
    }

    private void UnsubscribeModule(Module module)
    {
        if (!_subscribedModules.Remove(module))
        {
            return;
        }

        module.Items.CollectionChanged -= OnModuleItemsChanged;
        module.PropertyChanged -= OnModulePropertyChanged;
        module.BulkItemsLoadCompleted -= OnModuleBulkItemsLoadCompleted;
    }

    private void UnsubscribeAllModules()
    {
        foreach (var module in _subscribedModules.ToList())
        {
            UnsubscribeModule(module);
        }
    }

    private void OnModulePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (sender is Module module
            && e.PropertyName is nameof(Module.Name) or nameof(Module.SearchString) or nameof(Module.DiagnosticFilter))
        {
            RebuildEntryRows(module);
        }
    }

    private void OnModuleItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not ObservableCollection<TranslationEntry> items)
        {
            return;
        }

        var module = FindModuleForItems(items);
        if (module == null)
        {
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Reset or NotifyCollectionChangedAction.Replace:
                if (!IsPopulating(module))
                {
                    RebuildEntryRows(module);
                }

                break;
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems == null)
                {
                    break;
                }

                if (module.IsBulkItemsLoading && !UsesIncrementalBulkDisplay(module))
                {
                    break;
                }

                foreach (TranslationEntry entry in e.NewItems)
                {
                    TryInsertEntryRow(module, entry);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems == null)
                {
                    break;
                }

                foreach (TranslationEntry entry in e.OldItems)
                {
                    RemoveEntryRow(module, entry);
                }

                break;
            default:
                if (!IsPopulating(module))
                {
                    RebuildEntryRows(module);
                }

                break;
        }
    }

    private void TryInsertEntryRow(Module module, TranslationEntry entry)
    {
        if (entry == null || !_store.DisplayModules.Contains(module))
        {
            return;
        }

        if (!module.Items.Contains(entry))
        {
            return;
        }

        // Строки с диска приходят по одной во время загрузки модуля, поэтому активный фильтр
        // (поиск или диагностика) проверяется здесь: иначе модуль, открытый при непустой строке
        // поиска, наполнялся бы всеми строками в обход фильтра. Строки, добавленные пользователем,
        // показываются всегда — иначе новая пустая строка молча не появилась бы в гриде.
        if (module.IsBulkItemsLoading
            && !SearchEngine.PassesFilter(entry, module.SearchString, module.DiagnosticFilter))
        {
            return;
        }

        if (WorkspaceGridLayoutHelper.FindHeaderIndex(Rows, module) < 0)
        {
            return;
        }

        // Дедупликация только по ссылке: CollectionChanged и инкрементальное наполнение могут
        // попытаться вставить один и тот же объект дважды. По имени сравнивать нельзя —
        // пользователь может создать запись с уже занятым именем (например, «добавить строку ниже»
        // с автоинкрементом суффикса), и такая строка обязана появиться в гриде, чтобы было видно
        // подсвеченный валидатором дубликат.
        if (Rows.Any(r => r is TranslationEntryGridRow row
                          && ReferenceEquals(row.Module, module)
                          && ReferenceEquals(row.Entry, entry)))
        {
            return;
        }

        var insertIndex = WorkspaceGridLayoutHelper.GetEntryRowInsertIndex(Rows, module, entry);
        Rows.Insert(insertIndex, new TranslationEntryGridRow(module, entry));
    }

    private void RemoveEntryRow(Module module, TranslationEntry entry)
    {
        var index = WorkspaceGridLayoutHelper.IndexOfEntryRow(Rows, module, entry);
        if (index >= 0)
        {
            if (Rows[index] is TranslationEntryGridRow entryRow)
            {
                entryRow.Detach();
            }

            Rows.RemoveAt(index);
        }
    }

    private void RebuildEntryRows(Module module)
    {
        if (!_store.DisplayModules.Contains(module))
        {
            return;
        }

        var headerIndex = WorkspaceGridLayoutHelper.FindHeaderIndex(Rows, module);
        if (headerIndex < 0)
        {
            AttachModule(module, Rows.Count);
            return;
        }

        WorkspaceGridLayoutHelper.RemoveModuleEntryRows(Rows, module, headerIndex);
        StartPopulateVisibleEntries(module);
    }

    private void OnModuleBulkItemsLoadCompleted(object sender, EventArgs e)
    {
        if (sender is not Module module || !_store.DisplayModules.Contains(module))
        {
            return;
        }

        RefreshModuleEntryRowPresentation(module);

        if (!UsesIncrementalBulkDisplay(module) && !HasAllVisibleEntryRows(module))
        {
            StartPopulateVisibleEntries(module);
        }
        else
        {
            RepeatPendingScroll(module, complete: true);
        }

        if (!ReferenceEquals(_store.SelectedModule, module) || _store.SelectedTranslationEntry != null)
        {
            return;
        }

        var firstEntry = ModuleViewHelper.GetVisibleEntries(module).FirstOrDefault();
        if (firstEntry != null)
        {
            _store.SelectedTranslationEntry = firstEntry;
        }
    }

    private bool HasAllVisibleEntryRows(Module module)
    {
        var visible = ModuleViewHelper.GetVisibleEntries(module);
        if (visible.Count == 0)
        {
            return true;
        }

        foreach (var entry in visible)
        {
            if (!Rows.Any(r => r is TranslationEntryGridRow row && ReferenceEquals(row.Entry, entry)))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPopulating(Module module) => _populateCts.ContainsKey(module);

    private Module FindModuleForItems(ObservableCollection<TranslationEntry> items) =>
        _subscribedModules.FirstOrDefault(module => ReferenceEquals(module.Items, items))
        ?? _store.DisplayModules.FirstOrDefault(module => ReferenceEquals(module.Items, items));

    private bool UsesIncrementalBulkDisplay(Module module) =>
        _store.OpenModules.Contains(module) || _store.IsDiagnosticResultsView;

    private void RefreshModuleEntryRowPresentation(Module module)
    {
        foreach (var row in Rows.OfType<TranslationEntryGridRow>())
        {
            if (ReferenceEquals(row.Module, module))
            {
                row.RefreshRowPresentation();
            }
        }
    }

    private bool IsAlternateModuleView =>
        _store.IsSearchResultsView || _store.IsDiagnosticResultsView;
}