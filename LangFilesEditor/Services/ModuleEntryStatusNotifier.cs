namespace LangFilesEditor.Services;

using Models;

/// <summary>
/// Переводит события <see cref="Module.EntryAdded"/> в сообщения status bar.
/// </summary>
public sealed class ModuleEntryStatusNotifier
{
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
    
    // todo: это можно сделать в самом модуле. Но даже так, посмотря на метод FormatUserMessage
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
            if (e.Context.BulkProgress is { } progress)
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
            // todo: локализация
            TranslationEntryAddSource.User => $"Добавлен ключ «{e.Entry.Name}» в «{module.Name}»",
            TranslationEntryAddSource.ExternalLoader => $"Импорт из кода: «{e.Entry.Name}» в «{module.Name}»",
            TranslationEntryAddSource.Import => $"Импорт строки «{e.Entry.Name}» в «{module.Name}»",
            _ => null,
        };
    }
}