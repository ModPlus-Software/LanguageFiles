namespace LangFilesEditor.Core.Abstractions;

/// <summary>
/// Репортер прогресса длительной операции.
/// Все вызовы потокобезопасны — их можно выполнять из фонового потока.
/// </summary>
public interface IEditorOperationProgress
{
    /// <summary>
    /// Сообщает прогресс операции; общий индикатор показывает суммарную долю выполнения.
    /// </summary>
    /// <param name="current">Число выполненных единиц работы.</param>
    /// <param name="total">Общее число единиц работы; неположительное значение — неопределённый прогресс.</param>
    void Report(int current, int total);

    /// <summary>
    /// Меняет заголовок операции в status bar и во всплывающем списке.
    /// </summary>
    /// <param name="title">Новый заголовок операции.</param>
    void SetTitle(string title);
}