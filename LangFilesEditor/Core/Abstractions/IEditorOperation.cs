namespace LangFilesEditor.Core.Abstractions;

using System.ComponentModel;

/// <summary>
/// Представление одной длительной операции редактора (только чтение на данный момент).
/// </summary>
public interface IEditorOperation : INotifyPropertyChanged
{
    /// <summary>
    /// Заголовок операции без счётчика (например, «Загрузка «Common»»).
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Полный текст операции для отображения, включая счётчик при наличии прогресса.
    /// </summary>
    string DisplayText { get; }

    /// <summary>
    /// Текущее число выполненных единиц работы.
    /// </summary>
    int Current { get; }

    /// <summary>
    /// Ожидаемое общее число единиц работы; 0 — прогресс неизвестен.
    /// </summary>
    int Total { get; }

    /// <summary>
    /// Неопределённый ли прогресс (общее число единиц неизвестно).
    /// </summary>
    bool IsIndeterminate { get; }

    /// <summary>
    /// Доля выполнения от 0 до 1; 0 при неизвестном прогрессе.
    /// </summary>
    double Fraction { get; }
}