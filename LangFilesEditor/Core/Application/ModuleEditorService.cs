namespace LangFilesEditor.Core.Application;

using Abstractions;
using Models;

/// <summary>
/// Сервис редактирования модулей.
/// </summary>
public sealed class ModuleEditorService : IModuleEditor
{
    /// <inheritdoc />
    public Module GetOrCreateModule(Domain domain, string moduleName, string sourceFileName)
    {
        var existing = domain.Modules.FirstOrDefault(m =>
            string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            return existing;
        }

        var module = new Module(moduleName, domain, sourceFileName);
        domain.Modules.Add(module);

        return module;
    }

    /// <inheritdoc />
    public void AddTranslationEntry(Module module, TranslationEntry entry, TranslationEntryAddSource source) =>
        RunOnUiThread(() => module.AddTranslationEntry(entry, source));

    /// <inheritdoc />
    public void MergeTranslationEntries(Module module, IReadOnlyList<TranslationEntry> entries) =>
        RunOnUiThread(() =>
        {
            foreach (var entry in entries)
            {
                var existing = module.Items.FirstOrDefault(i =>
                    string.Equals(i.Name, entry.Name, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    module.AddTranslationEntry(entry, TranslationEntryAddSource.ExternalLoader);
                    continue;
                }

                MergeExistingEntry(existing, entry);
            }
        });

    private static void MergeExistingEntry(TranslationEntry existing, TranslationEntry scanned)
    {
        if (string.IsNullOrWhiteSpace(existing.Comment) && !string.IsNullOrWhiteSpace(scanned.Comment))
        {
            existing.Comment = scanned.Comment;
        }

        if (existing.Values.TryGetValue("ru-RU", out var ru) &&
            string.IsNullOrWhiteSpace(ru.Value) &&
            scanned.Values.TryGetValue("ru-RU", out var scannedRu))
        {
            ru.Value = scannedRu.Value;
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;

        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}