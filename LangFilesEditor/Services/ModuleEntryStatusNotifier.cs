namespace LangFilesEditor.Services;

using Helpers;
using Models;

/// <summary>
/// Переводит события <see cref="Module.EntryAdded"/> в сообщения status bar.
/// </summary>
public sealed class ModuleEntryStatusNotifier
{
    // Прогресс обновляется не на каждой строке: каждое обновление перерисовывает status bar,
    // а строки при загрузке модуля добавляются сотнями. Шаг мельче размера порции строк,
    // чтобы значение в status bar менялось хотя бы раз за отрисованный кадр.
    private const int ProgressReportStep = 4;

    private readonly EditorOperationTracker _operations;
    private readonly HashSet<Module> _attached = [];

    /// <summary>
    /// Создаёт notifier, публикующий сообщения в указанный трекер операций.
    /// </summary>
    /// <param name="operations">Трекер длительных операций status bar.</param>
    public ModuleEntryStatusNotifier(EditorOperationTracker operations)
    {
        _operations = operations;
    }

    /// <summary>
    /// Подписывает модуль на уведомления о добавлении записей.
    /// </summary>
    /// <param name="module">Модуль для отслеживания.</param>
    public void Attach(Module module)
    {
        if (module == null || !_attached.Add(module))
        {
            return;
        }

        module.EntryAdded += OnEntryAdded;
    }

    /// <summary>
    /// Подписывает все модули из перечисления на уведомления.
    /// </summary>
    /// <param name="modules">Модули для отслеживания.</param>
    public void AttachAll(IEnumerable<Module> modules)
    {
        foreach (var module in modules)
        {
            Attach(module);
        }
    }

    private void OnEntryAdded(object? sender, TranslationEntryAddedEventArgs e)
    {
        if (sender is not Module module)
        {
            return;
        }

        if (e.Context.Source == TranslationEntryAddSource.FileRepository)
        {
            if (e.Context.BulkProgress is { } progress
                && (progress.Current % ProgressReportStep == 0 || progress.Current >= progress.Total))
            {
                _operations.ReportByKey(module.Name, progress.Current, progress.Total);
            }

            return;
        }

        var message = FormatUserMessage(module, e);
        if (message != null)
        {
            _operations.PublishTransient(message);
        }
    }

    private static string FormatUserMessage(Module module, TranslationEntryAddedEventArgs e)
    {
        return e.Context.Source switch
        {
            TranslationEntryAddSource.User => EditorStrings.FormatEntryAddedByUser(e.Entry.Name, module.Name),
            TranslationEntryAddSource.ExternalLoader =>
                EditorStrings.FormatEntryImportedFromCode(e.Entry.Name, module.Name),
            TranslationEntryAddSource.Import => EditorStrings.FormatEntryImported(e.Entry.Name, module.Name),
            _ => null,
        };
    }
}