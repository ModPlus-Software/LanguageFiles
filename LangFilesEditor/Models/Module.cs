namespace LangFilesEditor.Models;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Services;
using Utils;
using ModPlusAPI.Mvvm;

/// <summary>
/// Модуль локализации: набор ключей перевода, метаданных и фильтрации для UI.
/// </summary>
public class Module : ObservableObject
{
    // Валидатор без состояния — один общий экземпляр на все модули.
    private static readonly Validator EntryValidator = new();

    // Записи, на события которых модуль уже подписан. Нужен, чтобы снимать подписки при удалении
    // и очистке коллекций: без этого записи удерживались бы модулем и валидация вызывалась бы
    // по несколько раз на одно изменение при повторном добавлении той же записи.
    private readonly HashSet<TranslationEntry> _validatedItems = [];
    private readonly HashSet<TranslationEntry> _validatedMetadata = [];
    private bool _hasIncorrectData;
    private bool _itemsHaveIncorrectData;
    private bool _metadataHaveIncorrectData;
    private int _errorCount;
    private int _warningCount;
    private int _catalogEntryCount;
    private string _searchString = string.Empty;
    private DiagnosticSeverity? _diagnosticFilter;
    private ICollectionView _itemsView;
    private int _bulkItemsLoadDepth;
    private ModuleItemsLoadState _itemsLoadState;

    /// <summary>
    /// Идёт пакетная загрузка items с диска; UI может отложить построчное отображение до завершения.
    /// </summary>
    public bool IsBulkItemsLoading => _bulkItemsLoadDepth > 0;

    /// <summary>
    /// Насколько полно загружены строки модуля в память редактора.
    /// </summary>
    public ModuleItemsLoadState ItemsLoadState
    {
        get => _itemsLoadState;
        private set
        {
            if (_itemsLoadState == value)
            {
                return;
            }

            _itemsLoadState = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Завершена пакетная загрузка items с диска.
    /// </summary>
    public event EventHandler BulkItemsLoadCompleted;

    /// <summary>
    /// Создаёт модуль с указанным именем, доменом и исходным XML-файлом.
    /// </summary>
    /// <param name="name">Имя модуля.</param>
    /// <param name="group">Родительский домен.</param>
    /// <param name="sourceFileName">Имя XML-файла без расширения; пустая строка, если не задано.</param>
    public Module(string name, Domain group, string sourceFileName = "")
    {
        Name = name;
        Group = group;
        SourceFileName = sourceFileName ?? string.Empty;
        Items = [];
        Items.CollectionChanged += ItemsOnCollectionChanged;
        Metadata = [];
        Metadata.CollectionChanged += MetadataOnCollectionChanged;
    }

    /// <summary>
    /// Родительский домен модуля.
    /// </summary>
    public Domain Group { get; }

    /// <summary>
    /// Имя модуля.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Имя XML-файла без расширения, в котором живёт модуль (источник на диске).
    /// </summary>
    public string SourceFileName { get; }

    /// <summary>
    /// Атрибуты модуля (метаданные XML-узла).
    /// </summary>
    public ObservableCollection<TranslationEntry> Metadata { get; }

    /// <summary>
    /// Items. Мутации — только через <see cref="AddTranslationEntry"/>,
    /// <see cref="InsertTranslationEntry"/>, <see cref="RemoveTranslationEntry"/>,
    /// <see cref="ClearTranslationEntries"/>.
    /// </summary>
    public ObservableCollection<TranslationEntry> Items { get; }

    /// <summary>
    /// Срабатывает после добавления entry через API модуля.
    /// </summary>
    public event EventHandler<TranslationEntryAddedEventArgs> EntryAdded;

    /// <summary>
    /// Отфильтрованный вид коллекции для грида (создаётся на UI-потоке при первом обращении).
    /// </summary>
    public ICollectionView ItemsView
    {
        get
        {
            if (_itemsView != null)
            {
                return _itemsView;
            }

            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.CheckAccess())
            {
                _itemsView = CreateItemsView();
                return _itemsView;
            }

            return dispatcher.Invoke(CreateItemsView);
        }
    }

    /// <summary>
    /// Строка текстового поиска для фильтрации записей в гриде. Независима от <see cref="DiagnosticFilter"/> —
    /// это два самостоятельных режима фильтрации, не кодируются одним значением.
    /// </summary>
    public string SearchString
    {
        get => _searchString;
        set
        {
            if (_searchString == value)
            {
                return;
            }

            _searchString = value;
            OnPropertyChanged();
            Search();
        }
    }

    /// <summary>
    /// Активный фильтр диагностики для записей в гриде (независимо от текстового поиска <see cref="SearchString"/>).
    /// </summary>
    public DiagnosticSeverity? DiagnosticFilter
    {
        get => _diagnosticFilter;
        set
        {
            if (_diagnosticFilter == value)
            {
                return;
            }

            _diagnosticFilter = value;
            OnPropertyChanged();
            Search();
        }
    }

    /// <summary>
    /// Полное число TranslationEntry модуля. Берётся из каталога после сканирования
    /// и не меняется по мере подгрузки <see cref="Items"/>; растёт, только если
    /// в модуль фактически добавлены новые элементы поверх каталога.
    /// </summary>
    public int EntryCount => Math.Max(_catalogEntryCount, Items.Count);

    /// <summary>
    /// Содержит ли модуль хотя бы одну запись с некорректными данными.
    /// </summary>
    public bool HasIncorrectData
    {
        get => _hasIncorrectData;
        private set
        {
            if (_hasIncorrectData == value)
            {
                return;
            }

            _hasIncorrectData = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Число загруженных строк (и атрибутов) модуля с ошибками валидации.
    /// </summary>
    public int ErrorCount
    {
        get => _errorCount;
        private set
        {
            if (_errorCount == value)
            {
                return;
            }

            _errorCount = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Число загруженных строк (и атрибутов) модуля с предупреждениями валидации.
    /// </summary>
    public int WarningCount
    {
        get => _warningCount;
        private set
        {
            if (_warningCount == value)
            {
                return;
            }

            _warningCount = value;
            OnPropertyChanged();
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Устанавливает число записей из каталога репозитория до полной загрузки items.
    /// </summary>
    internal void SetCatalogEntryCount(int count)
    {
        if (_catalogEntryCount == count)
        {
            return;
        }

        _catalogEntryCount = count;

        if (Items.Count == 0)
        {
            OnPropertyChanged(nameof(EntryCount));
        }
    }

    /// <summary>
    /// Откладывает полную валидацию items при пакетной загрузке из файлов.
    /// </summary>
    internal void BeginBulkItemsLoad() => _bulkItemsLoadDepth++;

    /// <summary>
    /// Завершает один уровень пакетной загрузки и запускает валидацию при выходе на ноль.
    /// </summary>
    internal void EndBulkItemsLoad()
    {
        if (_bulkItemsLoadDepth <= 0)
        {
            return;
        }

        _bulkItemsLoadDepth--;

        if (_bulkItemsLoadDepth != 0)
        {
            return;
        }

        RunItemsValidation();
        BulkItemsLoadCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Добавляет запись и уведомляет подписчиков (status bar и др.).
    /// </summary>
    /// <param name="entry">Добавляемая запись перевода.</param>
    /// <param name="source">Источник добавления.</param>
    /// <param name="bulkProgress">Прогресс пакетной загрузки; передаётся при чтении с диска.</param>
    public void AddTranslationEntry(
        TranslationEntry entry,
        TranslationEntryAddSource source,
        BulkLoadProgress? bulkProgress = null)
    {
        if (source == TranslationEntryAddSource.FileRepository && SearchEngine.ContainsItemByName(Items, entry.Name))
        {
            return;
        }

        Items.Add(entry);
        RaiseEntryAdded(entry, source, bulkProgress);
    }

    /// <summary>
    /// Вставляет запись по индексу и уведомляет подписчиков.
    /// </summary>
    /// <param name="index">Индекс вставки в коллекции <see cref="Items"/>.</param>
    /// <param name="entry">Вставляемая запись перевода.</param>
    /// <param name="source">Источник добавления.</param>
    public void InsertTranslationEntry(int index, TranslationEntry entry, TranslationEntryAddSource source)
    {
        Items.Insert(index, entry);
        RaiseEntryAdded(entry, source);
    }

    /// <summary>
    /// Удаляет запись из коллекции.
    /// </summary>
    /// <param name="entry">Удаляемая запись перевода.</param>
    public void RemoveTranslationEntry(TranslationEntry entry) => Items.Remove(entry);

    /// <summary>
    /// Очищает коллекцию записей перевода.
    /// </summary>
    public void ClearTranslationEntries() => Items.Clear();

    /// <summary>
    /// Добавляет атрибут модуля в коллекцию <see cref="Metadata"/>.
    /// </summary>
    /// <param name="entry">Запись атрибута.</param>
    public void AddMetadataEntry(TranslationEntry entry) => Metadata.Add(entry);

    /// <summary>
    /// Поштучно наполняет модуль после чтения с диска: metadata сразу, items — с yield UI.
    /// </summary>
    /// <param name="metadata">Атрибуты модуля из репозитория.</param>
    /// <param name="items">Записи перевода из репозитория.</param>
    /// <param name="cancellationToken">Токен отмены загрузки.</param>
    public async Task PopulateFromRepositoryAsync(
        IReadOnlyList<TranslationEntry> metadata,
        IReadOnlyList<TranslationEntry> items,
        CancellationToken cancellationToken = default)
    {
        if (ItemsLoadState == ModuleItemsLoadState.Full)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher
                         ?? throw new InvalidOperationException("UI dispatcher is not available.");

        if (!dispatcher.CheckAccess())
        {
            await await dispatcher.InvokeAsync(() =>
                PopulateFromRepositoryAsync(metadata, items, cancellationToken));
            return;
        }

        ResetIncompleteLoad();

        BeginBulkItemsLoad();

        try
        {
            foreach (var attribute in metadata)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddMetadataEntry(attribute);
            }

            var total = Math.Max(Math.Max(items.Count, EntryCount), 1);
            var yieldBudget = new UiYieldBudget(
                EditorSettingsStore.Instance.Current.InitialRowsPerFrame,
                EditorSettingsStore.Instance.Current.MaxRowsPerFrame);

            for (var i = 0; i < items.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddTranslationEntry(
                    items[i],
                    TranslationEntryAddSource.FileRepository,
                    new BulkLoadProgress(i + 1, total));

                if (!yieldBudget.RegisterRow())
                {
                    continue;
                }

                await dispatcher.YieldAsync(DispatcherPriority.Background);
                yieldBudget.Reset();
            }

            ItemsLoadState = ModuleItemsLoadState.Full;
        }
        finally
        {
            EndBulkItemsLoad();
        }
    }

    /// <summary>
    /// Загружает в модуль только строки, подходящие под фильтр диагностики (без полного чтения в UI).
    /// </summary>
    /// <param name="items">Все строки модуля, прочитанные с диска.</param>
    /// <param name="severity">Категория диагностики.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task PopulateDiagnosticEntriesAsync(
        IReadOnlyList<TranslationEntry> items,
        DiagnosticSeverity severity,
        CancellationToken cancellationToken = default)
    {
        if (ItemsLoadState == ModuleItemsLoadState.Full)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher
                         ?? throw new InvalidOperationException("UI dispatcher is not available.");

        if (!dispatcher.CheckAccess())
        {
            await await dispatcher.InvokeAsync(() =>
                PopulateDiagnosticEntriesAsync(items, severity, cancellationToken));
            return;
        }

        if (ItemsLoadState == ModuleItemsLoadState.PartialDiagnostic)
        {
            ClearTranslationEntries();
            ResetItemsView();
        }

        if (IsBulkItemsLoading)
        {
            return;
        }

        var matching = SelectDiagnosticEntries(items, severity);
        if (matching.Count == 0)
        {
            ItemsLoadState = ModuleItemsLoadState.None;
            return;
        }

        BeginBulkItemsLoad();

        try
        {
            var total = Math.Max(matching.Count, 1);
            var yieldBudget = new UiYieldBudget(
                Services.EditorSettingsStore.Instance.Current.InitialRowsPerFrame,
                Services.EditorSettingsStore.Instance.Current.MaxRowsPerFrame);
            var addedNames = new HashSet<string>(Items.Select(item => item.Name), StringComparer.Ordinal);

            for (var i = 0; i < matching.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = matching[i];
                if (!addedNames.Add(entry.Name))
                {
                    continue;
                }

                AddTranslationEntry(
                    entry,
                    TranslationEntryAddSource.FileRepository,
                    new BulkLoadProgress(i + 1, total));

                if (!yieldBudget.RegisterRow())
                {
                    continue;
                }

                await dispatcher.YieldAsync(DispatcherPriority.Background);
                yieldBudget.Reset();
            }

            ItemsLoadState = ModuleItemsLoadState.PartialDiagnostic;
        }
        finally
        {
            EndBulkItemsLoad();
        }
    }

    /// <summary>
    /// Сбрасывает частичную диагностическую загрузку, если модуль не открыт полностью.
    /// </summary>
    internal void ClearPartialDiagnosticLoad()
    {
        if (ItemsLoadState != ModuleItemsLoadState.PartialDiagnostic)
        {
            return;
        }

        ResetIncompleteLoad();
    }

    /// <summary>
    /// Сбрасывает неполное наполнение модуля (частичная диагностика, прерванная загрузка).
    /// </summary>
    internal void ResetIncompleteLoad()
    {
        if (ItemsLoadState == ModuleItemsLoadState.Full)
        {
            return;
        }

        ClearTranslationEntries();
        Metadata.Clear();
        ResetItemsView();
        ItemsLoadState = ModuleItemsLoadState.None;
        ErrorCount = 0;
        WarningCount = 0;
    }

    private void ResetItemsView()
    {
        if (_itemsView == null)
        {
            return;
        }

        _itemsView.Filter = null;
        _itemsView = null;
    }

    private void RaiseEntryAdded(
        TranslationEntry entry,
        TranslationEntryAddSource source,
        BulkLoadProgress? bulkProgress = null)
    {
        EntryAdded?.Invoke(this, new TranslationEntryAddedEventArgs(
            entry,
            new TranslationEntryAddContext(source, bulkProgress)));
    }

    private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(EntryCount));
        UpdateValidationSubscriptions(e, Items, _validatedItems, OnItemValidateInParent);

        if (_bulkItemsLoadDepth > 0)
        {
            return;
        }

        RunItemsValidation();
    }

    private void OnItemValidateInParent(object sender, EventArgs e) => RunItemsValidation();

    private void OnMetadataValidateInParent(object sender, EventArgs e) => RunMetadataValidation();

    /// <summary>
    /// Синхронизирует подписки на <see cref="TranslationEntry.ValidateInParent"/> с составом коллекции.
    /// </summary>
    private static void UpdateValidationSubscriptions(
        NotifyCollectionChangedEventArgs e,
        IEnumerable<TranslationEntry> currentEntries,
        HashSet<TranslationEntry> subscribed,
        EventHandler handler)
    {
        // Reset не сообщает удалённые элементы, поэтому подписки пересобираются целиком.
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var entry in subscribed)
            {
                entry.ValidateInParent -= handler;
            }

            subscribed.Clear();
            foreach (var entry in currentEntries)
            {
                if (subscribed.Add(entry))
                {
                    entry.ValidateInParent += handler;
                }
            }

            return;
        }

        if (e.OldItems != null)
        {
            foreach (var entry in e.OldItems.OfType<TranslationEntry>())
            {
                if (subscribed.Remove(entry))
                {
                    entry.ValidateInParent -= handler;
                }
            }
        }

        if (e.NewItems == null)
        {
            return;
        }

        foreach (var entry in e.NewItems.OfType<TranslationEntry>())
        {
            if (subscribed.Add(entry))
            {
                entry.ValidateInParent += handler;
            }
        }
    }

    private void RunItemsValidation()
    {
        _itemsHaveIncorrectData = EntryValidator.ValidateItems(Items);
        UpdateHasIncorrectData();

        if (DiagnosticFilter.HasValue && _itemsView != null)
        {
            _itemsView.Refresh();
        }
    }

    private static List<TranslationEntry> SelectDiagnosticEntries(
        IReadOnlyList<TranslationEntry> items,
        DiagnosticSeverity severity)
    {
        var itemList = items as IList<TranslationEntry> ?? items.ToList();
        EntryValidator.ValidateItems(itemList);

        return itemList
            .Where(entry => entry.DiagnosticState.MatchesDiagnosticFilter(severity))
            .GroupBy(entry => entry.Name, StringComparer.Ordinal)
            .Select(group => group.First())
            .Select(entry =>
            {
                entry.DiagnosticState.HasDuplicateName = false;
                entry.DiagnosticState.HasDuplicateValue = false;
                entry.Validate();
                return entry;
            })
            .ToList();
    }

    private void RunMetadataValidation()
    {
        _metadataHaveIncorrectData = EntryValidator.ValidateAttributes(Metadata);
        UpdateHasIncorrectData();
    }

    private void UpdateHasIncorrectData()
    {
        HasIncorrectData = _itemsHaveIncorrectData || _metadataHaveIncorrectData;
        UpdateDiagnosticCounts();
    }

    private void UpdateDiagnosticCounts()
    {
        var errors = 0;
        var warnings = 0;
        foreach (var item in Items)
        {
            if (item.DiagnosticState.IsVisibleError)
            {
                errors++;
            }

            if (item.DiagnosticState.IsVisibleWarning)
            {
                warnings++;
            }
        }

        foreach (var attribute in Metadata)
        {
            if (attribute.DiagnosticState.IsVisibleError)
            {
                errors++;
            }

            if (attribute.DiagnosticState.IsVisibleWarning)
            {
                warnings++;
            }
        }

        ErrorCount = errors;
        WarningCount = warnings;
    }

    private void MetadataOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateValidationSubscriptions(e, Metadata, _validatedMetadata, OnMetadataValidateInParent);
        RunMetadataValidation();
    }

    private void Search()
    {
        if (_itemsView != null)
        {
            _itemsView.Refresh();
        }
    }

    /// <summary>
    /// Есть ли строки, проходящие текущий фильтр (<see cref="SearchString"/> или <see cref="DiagnosticFilter"/>).
    /// </summary>
    /// <returns><c>true</c>, если фильтр не пуст и хотя бы одна запись видима, либо фильтр пуст и есть записи.</returns>
    public bool HasVisibleSearchResults
    {
        get
        {
            if (string.IsNullOrEmpty(SearchString) && !DiagnosticFilter.HasValue)
            {
                return Items.Count > 0;
            }

            var view = ItemsView;
            if (view != null)
            {
                foreach (var _ in view)
                {
                    return true;
                }

                return false;
            }

            return Items.Any(PassesSearchFilter);
        }
    }

    private ICollectionView CreateItemsView()
    {
        if (_itemsView != null)
        {
            return _itemsView;
        }

        var view = CollectionViewSource.GetDefaultView(Items);
        view.Filter = PassesSearchFilter;
        _itemsView = view;

        return _itemsView;
    }

    private bool PassesSearchFilter(object obj) =>
        obj is TranslationEntry item && SearchEngine.PassesFilter(item, SearchString, DiagnosticFilter);
}