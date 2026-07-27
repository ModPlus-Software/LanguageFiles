namespace LangFilesEditor.Core.Abstractions;

using System.ComponentModel;

// todo: можно потом как доделаю это подключить AI сервис к этой штуке
/// <summary>
/// Представление одной длительной операции редактора (только чтение на данный момент).
/// </summary>
public interface IEditorOperation : INotifyPropertyChanged
{
    // todo: Мб оставить только Title, а DisplayText убрать?
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

    // todo: я бы переименовал с fraction на что-то ещё. Если честно. Мне кажется не настолько информативным название
    /// <summary>
    /// Доля выполнения от 0 до 1; 0 при неизвестном прогрессе.
    /// </summary>
    double Fraction { get; }
}