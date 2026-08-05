namespace LangFilesEditor.Services.Diagnostics;

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Core.Abstractions;
using Helpers;
using Models;
using ModPlusAPI.Mvvm;

/// <summary>
/// Сводка диагностики редактора: собирает проблемы локализации по модулям, группирует их
/// по категориям для UI и принимает публикации от расширений.
/// </summary>
public sealed class EditorDiagnosticsService : ObservableObject, IEditorDiagnostics, IDiagnosticsPublisher
{
    // Маркер категории в status bar — залитый круг (U+25CF); цвет задаётся стилем по severity.
    private const string CategoryMarker = "\u25CF";

    private readonly EntryDiagnosticsStore _entryDiagnostics = new();
    private readonly EditorOperationTracker _operations;
    private readonly LocalizationDiagnosticsScanner _scanner = new();
    private readonly Dictionary<string, List<EditorDiagnostic>> _bySource = new(StringComparer.Ordinal);
    private readonly Dictionary<Module, ModuleDiagnosticCounts> _startupScanCounts = new();
    private readonly HashSet<Module> _observedModules = [];
    private readonly HashSet<TranslationEntry> _highlightedEntries = [];
    private readonly System.Windows.Input.ICommand _selectModuleCommand;
    private IEditorSession _session;
    private IEditorWorkspace _workspace;
    private CancellationTokenSource _startupScanCts;
    private bool _isScanning;
    private bool _recomputeScheduled;

    /// <summary>
    /// Создаёт сервис диагностики без привязки к сессии — она подключается отдельным вызовом
    /// <see cref="AttachToSession"/>, чтобы слой диагностики можно было создавать и подключать
    /// в нужной точке процесса, а не только в момент конструирования вместе с сессией.
    /// </summary>
    /// <param name="operations">Трекер прогресса длительных операций.</param>
    public EditorDiagnosticsService(EditorOperationTracker operations)
    {
        _operations = operations;
        _selectModuleCommand = new RelayCommand<DiagnosticModuleEntry>(SelectModule);
        Errors = new DiagnosticCategory(
            DiagnosticSeverity.Error, EditorStrings.DiagnosticCategoryErrors, CategoryMarker);
        Warnings = new DiagnosticCategory(
            DiagnosticSeverity.Warning, EditorStrings.DiagnosticCategoryWarnings, CategoryMarker);
        Updates = new DiagnosticCategory(
            DiagnosticSeverity.Update, EditorStrings.DiagnosticCategoryUpdates, CategoryMarker);
        Categories = [Errors, Warnings, Updates];
    }

    /// <summary>
    /// Подключает слой диагностики к сессии редактора: подписывается на изменения открытых модулей
    /// и выполняет первичный расчёт встроенной диагностики уже загруженных модулей. Явный hook-метод —
    /// вызывается тем, кто хочет включить диагностику для конкретной сессии (обычно bootstrap),
    /// а не автоматически при создании сервиса.
    /// </summary>
    /// <param name="session">Сессия редактора с доменами, за модулями которой нужно наблюдать.</param>
    /// <param name="workspace">Рабочая область с открытыми вкладками (для наблюдения и показа диагностики).</param>
    public void AttachToSession(IEditorSession session, IEditorWorkspace workspace)
    {
        if (_session != null || session == null || workspace == null)
        {
            return;
        }

        _session = session;
        _workspace = workspace;
        _workspace.OpenModules.CollectionChanged += OnOpenModulesChanged;
        NotifyModulesChanged();
    }

    /// <summary>
    /// Явный hook: сообщает слою диагностики, что состав модулей мог измениться (появились новые
    /// модули/домены), и нужно пересчитать встроенную диагностику. Может вызываться любым
    /// координатором в процессе работы приложения, а не только внутренней подпиской на сессию.
    /// </summary>
    public void NotifyModulesChanged()
    {
        if (_session == null)
        {
            return;
        }

        ObserveAllModules();
        Recompute();
    }

    /// <inheritdoc />
    public DiagnosticCategory Errors { get; }

    /// <inheritdoc />
    public DiagnosticCategory Warnings { get; }

    /// <inheritdoc />
    public DiagnosticCategory Updates { get; }

    /// <inheritdoc />
    public IReadOnlyList<DiagnosticCategory> Categories { get; }

    /// <inheritdoc />
    public bool HasAny => Errors.HasItems || Warnings.HasItems || Updates.HasItems;

    /// <summary>
    /// Выполняется ли сейчас сканирование диагностики на диске.
    /// </summary>
    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (_isScanning == value)
            {
                return;
            }

            _isScanning = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Фоновое сканирование всех модулей на диске после загрузки каталогов domain.
    /// </summary>
    /// <param name="repository">Репозиторий языковых файлов.</param>
    /// <param name="languages">Коды языков проекта.</param>
    public async Task RunStartupScanAsync(ILanguageRepository repository, IReadOnlyList<string> languages)
    {
        if (_session == null)
        {
            return;
        }

        // Предыдущий скан только отменяется: освободить его CTS должен он сам — иначе токен,
        // с которым прямо сейчас работает Parallel.ForEachAsync, был бы уничтожен из-под него.
        var scanCts = new CancellationTokenSource();
        var previousScanCts = Interlocked.Exchange(ref _startupScanCts, scanCts);
        try
        {
            previousScanCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // предыдущий скан уже успел завершиться и освободить свой источник отмены
        }

        var cancellationToken = scanCts.Token;

        IsScanning = true;
        try
        {
            var results = await _scanner.ScanAllAsync(
                repository,
                _session.Domains,
                languages,
                _operations,
                module => module.Items.Count > 0,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            RunOnUi(() =>
            {
                _startupScanCounts.Clear();
                foreach (var pair in results)
                {
                    _startupScanCounts[pair.Key] = pair.Value;
                }

                NotifyModulesChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // ожидаемо при повторном запуске или закрытии приложения
        }
        finally
        {
            // Индикатор гасит только актуальный скан: отменённый не должен сбрасывать флаг нового.
            if (Interlocked.CompareExchange(ref _startupScanCts, null, scanCts) == scanCts)
            {
                IsScanning = false;
            }

            scanCts.Dispose();
        }
    }

    /// <inheritdoc />
    public void Publish(string source, IEnumerable<EditorDiagnostic> diagnostics)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        var snapshot = diagnostics?.Where(d => d != null).ToList() ?? [];
        RunOnUi(() =>
        {
            _bySource[source] = snapshot;
            Recompute();
        });
    }

    /// <inheritdoc />
    public void Clear(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        RunOnUi(() =>
        {
            if (_bySource.Remove(source))
            {
                Recompute();
            }
        });
    }

    private static void RunOnUi(Action action) =>
        Utils.DispatcherExtensions.RunOnUiThread(Application.Current?.Dispatcher, action, DispatcherPriority.Send);

    private void OnOpenModulesChanged(object sender, NotifyCollectionChangedEventArgs e) => NotifyModulesChanged();

    private void ObserveAllModules()
    {
        foreach (var module in SearchEngine.CollectAllModules(_session.Domains))
        {
            if (_observedModules.Add(module))
            {
                module.PropertyChanged += OnModulePropertyChanged;
            }
        }
    }

    private void OnModulePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Module.ErrorCount) or nameof(Module.WarningCount))
        {
            ScheduleRecompute();
        }
    }

    /// <summary>
    /// Откладывает пересчёт сводки до простоя UI. Счётчики модулей меняются построчно при загрузке
    /// и сканировании, а <see cref="Recompute"/> каждый раз обходит все модули и пересоздаёт списки
    /// категорий, поэтому вызовы коалесцируются в один.
    /// </summary>
    private void ScheduleRecompute()
    {
        if (_recomputeScheduled)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            Recompute();
            return;
        }

        _recomputeScheduled = true;
        dispatcher.BeginInvoke(
            () =>
            {
                _recomputeScheduled = false;
                Recompute();
            },
            DispatcherPriority.Background);
    }

    private void Recompute()
    {
        var errorCounts = new Dictionary<Module, int>();
        var warningCounts = new Dictionary<Module, int>();
        var updateCounts = new Dictionary<Module, int>();

        foreach (var module in SearchEngine.CollectAllModules(_session.Domains))
        {
            var (errors, warnings) = GetBuiltInCounts(module);
            if (errors > 0)
            {
                errorCounts[module] = errors;
            }

            if (warnings > 0)
            {
                warningCounts[module] = warnings;
            }
        }

        foreach (var diagnostic in _bySource.Values.SelectMany(list => list))
        {
            var bucket = diagnostic.Severity switch
            {
                DiagnosticSeverity.Error => errorCounts,
                DiagnosticSeverity.Warning => warningCounts,
                _ => updateCounts,
            };

            bucket.TryGetValue(diagnostic.Module, out var current);
            bucket[diagnostic.Module] = current + 1;
        }

        ApplyCategory(Errors, errorCounts);
        ApplyCategory(Warnings, warningCounts);
        ApplyCategory(Updates, updateCounts);
        ApplyEntryHighlights();

        OnPropertyChanged(nameof(HasAny));
    }

    private (int errors, int warnings) GetBuiltInCounts(Module module)
    {
        if (module.Items.Count > 0 && !module.IsBulkItemsLoading)
        {
            return (module.ErrorCount, module.WarningCount);
        }

        return _startupScanCounts.TryGetValue(module, out var scan)
            ? (scan.Errors, scan.Warnings)
            : (0, 0);
    }

    private void SelectModule(DiagnosticModuleEntry entry)
    {
        if (entry?.Module == null)
        {
            return;
        }

        _ = _workspace.ShowModuleDiagnosticAsync(entry.Module, entry.Severity);
    }

    private void ApplyCategory(DiagnosticCategory category, Dictionary<Module, int> counts)
    {
        category.Modules.Clear();
        foreach (var pair in counts.OrderByDescending(p => p.Value).ThenBy(p => p.Key.Name, StringComparer.OrdinalIgnoreCase))
        {
            category.Modules.Add(new DiagnosticModuleEntry(pair.Key, category.Severity, _selectModuleCommand) { Count = pair.Value });
        }

        category.Count = counts.Values.Sum();
    }

    /// <summary>
    /// Пересчитывает диагностику расширений/скана по записям: <see cref="_entryDiagnostics"/> — источник
    /// истины, живущий независимо от <see cref="TranslationEntry"/>; поля на <see cref="TranslationEntry.DiagnosticState"/>
    /// заполняются из него только как проекция для текущего UI-биндинга подсветки строк грида.
    /// </summary>
    private void ApplyEntryHighlights()
    {
        foreach (var entry in _highlightedEntries)
        {
            _entryDiagnostics.Clear(entry);
            entry.DiagnosticState.ExtensionDiagnostic = null;
            entry.DiagnosticState.DiagnosticToolTip = null;
        }

        _highlightedEntries.Clear();

        var strongest = new Dictionary<TranslationEntry, EditorDiagnostic>();
        foreach (var diagnostic in _bySource.Values.SelectMany(list => list))
        {
            if (diagnostic.Entry == null)
            {
                continue;
            }

            if (!strongest.TryGetValue(diagnostic.Entry, out var existing)
                || Precedence(diagnostic.Severity) > Precedence(existing.Severity))
            {
                strongest[diagnostic.Entry] = diagnostic;
            }
        }

        foreach (var pair in strongest)
        {
            _entryDiagnostics.Set(pair.Key, pair.Value);
            pair.Key.DiagnosticState.ExtensionDiagnostic = pair.Value.Severity;
            pair.Key.DiagnosticState.DiagnosticToolTip = pair.Value.Message;
            _highlightedEntries.Add(pair.Key);
        }
    }

    /// <inheritdoc />
    public bool TryGetEntryDiagnostic(TranslationEntry entry, out EditorDiagnostic diagnostic) =>
        _entryDiagnostics.TryGet(entry, out diagnostic);

    private static int Precedence(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => 3,
        DiagnosticSeverity.Warning => 2,
        _ => 1,
    };
}