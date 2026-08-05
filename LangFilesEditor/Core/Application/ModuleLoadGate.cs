namespace LangFilesEditor.Core.Application;

using System.Collections.Concurrent;
using Models;

/// <summary>
/// Гейт «одна загрузка на модуль»: параллельные запросы на чтение строк одного модуля
/// выстраиваются в очередь, что исключает двойное наполнение <see cref="Module.Items"/>.
/// </summary>
internal sealed class ModuleLoadGate
{
    private readonly ConcurrentDictionary<Module, SemaphoreSlim> _gates = new();

    /// <summary>
    /// Выполняет <paramref name="loadAsync"/> под эксклюзивным гейтом указанного модуля.
    /// Если для модуля уже идёт загрузка, дожидается её освобождения перед выполнением.
    /// </summary>
    /// <param name="module">Модуль, для которого нужна эксклюзивность загрузки.</param>
    /// <param name="loadAsync">Асинхронное действие загрузки.</param>
    /// <param name="cancellationToken">Токен отмены ожидания гейта.</param>
    /// <returns>Результат <paramref name="loadAsync"/>.</returns>
    public async Task<T> RunAsync<T>(
        Module module,
        Func<Task<T>> loadAsync,
        CancellationToken cancellationToken = default)
    {
        var gate = _gates.GetOrAdd(module, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await loadAsync();
        }
        finally
        {
            gate.Release();
        }
    }
}