namespace LangFilesEditor.Services.Diagnostics;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Core.Abstractions;
using Helpers;
using Models;

// todo: то, что без наполнения и правильно и нет, но в любом случае это лишнее в описании. Это в remarks стоит записать, если вообще должно быть именно в этом классе
/// <summary>
/// Сканирует XML на диске и считает ошибки/предупреждения валидации без наполнения UI-модулей.
/// </summary>
public sealed class LocalizationDiagnosticsScanner
{
    // todo: строго говоря валидатор здесь бесполезен, потому что выполняет валидирование по дублированию, и то не самостоятельно проверяет, а на веру подчерпнутых моделей дааных, что не окей. Хотя он должен валидировать на же ошибки. Исохдя из всего этого строятся здесь неправильные методы. Почему-то  Есть метод сканирования модуля, но сканирование происходит в методе счёта диагностики, что полностью не соответствует их наименованиям. Те мбоолее нельзя, что бы у самих TrnaslationEntry хранилась информация об ошибке в самой себе. Это странно. Слишком умная словно TrnalsationEntry и хранит данные о своей валидированности. Проверка должна по-другому идти.
    private static readonly Validator Validator = new();
    
    /// <summary>
    /// Сканирует все модули доменов и возвращает счётчики диагностики по каждому модулю.
    /// </summary>
    /// <param name="repository">Репозиторий языковых файлов.</param>
    /// <param name="domains">Домены редактора с каталогами модулей.</param>
    /// <param name="languages">Коды языков проекта.</param>
    /// <param name="operations">Трекер прогресса для status bar.</param>
    /// <param name="shouldSkipModule">Модули, которые не нужно читать с диска (уже загружены в UI).</param>
    /// <param name="cancellationToken">Токен отмены сканирования.</param>
    /// <returns>Счётчики диагностики по модулям (только модули с проблемами).</returns>
    public async Task<IReadOnlyDictionary<Module, ModuleDiagnosticCounts>> ScanAllAsync(
        ILanguageRepository repository,
        ObservableCollection<Domain> domains,
        IReadOnlyList<string> languages,
        EditorOperationTracker operations,
        Func<Module, bool> shouldSkipModule,
        CancellationToken cancellationToken = default)
    {
        var allModules = SearchEngine.CollectAllModules(domains);
        var toScan = allModules.Where(module => !shouldSkipModule(module)).ToList();
        if (toScan.Count == 0)
        {
            return new Dictionary<Module, ModuleDiagnosticCounts>();
        }
        
        var results = new ConcurrentDictionary<Module, ModuleDiagnosticCounts>();
        var operation = operations.Begin(EditorStrings.ScanningDiagnostics, total: toScan.Count);
        var done = 0;
        
        try
        {
            await Parallel.ForEachAsync(
                toScan,
                new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
                async (module, ct) =>
                {
                    var counts = await ScanModuleAsync(repository, module, languages, ct);
                    if (counts.Errors > 0 || counts.Warnings > 0)
                    {
                        results[module] = counts;
                    }
                    
                    var completed = Interlocked.Increment(ref done);
                    operations.Report(operation, completed, toScan.Count);
                });
        }
        finally
        {
            operations.End(operation);
        }
        
        return results.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
    
    /// <summary>
    /// Подсчитывает ошибки и предупреждения по уже прочитанным записям (та же логика, что у <see cref="Module"/>).
    /// </summary>
    /// <param name="metadata">Атрибуты модуля.</param>
    /// <param name="items">Строки перевода модуля.</param>
    /// <returns>Суммарные счётчики по items и metadata.</returns>
    public static ModuleDiagnosticCounts CountDiagnostics(
        IReadOnlyList<TranslationEntry> metadata,
        IReadOnlyList<TranslationEntry> items)
    {
        var itemList = items as IList<TranslationEntry> ?? items.ToList();
        var metadataList = metadata as IList<TranslationEntry> ?? metadata.ToList();
        Validator.ValidateItems(itemList);
        Validator.ValidateAttributes(metadataList);
        
        var errors = 0;
        var warnings = 0;
        foreach (var entry in itemList)
        {
            if (entry.DiagnosticState.IsVisibleError)
            {
                errors++;
            }
            
            if (entry.DiagnosticState.IsVisibleWarning)
            {
                warnings++;
            }
        }
        
        foreach (var entry in metadataList)
        {
            if (entry.DiagnosticState.IsVisibleError)
            {
                errors++;
            }
            
            if (entry.DiagnosticState.IsVisibleWarning)
            {
                warnings++;
            }
        }
        
        return new ModuleDiagnosticCounts(errors, warnings);
    }
    
    private static async Task<ModuleDiagnosticCounts> ScanModuleAsync(
        ILanguageRepository repository,
        Module module,
        IReadOnlyList<string> languages,
        CancellationToken cancellationToken)
    {
        var data = await repository.ReadTranslationEntriesAsync(module, languages, cancellationToken);
        return CountDiagnostics(data.Metadata, data.Items);
    }
}