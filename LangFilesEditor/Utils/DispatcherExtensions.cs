namespace LangFilesEditor.Utils;

using System.Windows.Threading;

/// <summary>
/// Расширения для <see cref="Dispatcher"/>, упрощающие асинхронную работу с UI.
/// </summary>
internal static class DispatcherExtensions
{
    /// <summary>
    /// Уступает управление UI-потоку: ставит в очередь диспетчера пустую операцию и возвращает задачу
    /// её выполнения. Позволяет длинным циклам наполнения UI не блокировать отрисовку и ввод.
    /// </summary>
    /// <param name="dispatcher">Диспетчер UI-потока, через который выполняется отложенная операция.</param>
    /// <param name="priority">Приоритет постановки операции в очередь диспетчера.</param>
    /// <returns>Задача, завершающаяся после того, как диспетчер обработал очередь до указанного приоритета.</returns>
    public static Task YieldAsync(this Dispatcher dispatcher, DispatcherPriority priority = DispatcherPriority.Input) =>
        dispatcher.InvokeAsync(static () => { }, priority).Task;

    /// <summary>
    /// Выполняет <paramref name="action"/> синхронно на потоке <paramref name="dispatcher"/>: сразу,
    /// если вызов уже идёт на нём (или диспетчера нет, например в design-time), иначе — через
    /// <see cref="Dispatcher.Invoke(Action, DispatcherPriority)"/>. Единственное место с этой логикой:
    /// раньше она была продублирована в нескольких сервисах по отдельности.
    /// </summary>
    /// <param name="dispatcher">Диспетчер UI-потока; <see langword="null"/> — выполнить немедленно.</param>
    /// <param name="action">Действие для выполнения на UI-потоке.</param>
    /// <param name="priority">Приоритет постановки операции в очередь диспетчера.</param>
    public static void RunOnUiThread(Dispatcher dispatcher, Action action, DispatcherPriority priority)
    {
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, priority);
    }
}