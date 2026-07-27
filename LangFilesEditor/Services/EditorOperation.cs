namespace LangFilesEditor.Services;

using Core.Abstractions;
using ModPlusAPI.Mvvm;

/// <summary>
/// Отслеживаемая длительная операция редактора с прогрессом.
/// </summary>
public sealed class EditorOperation : ObservableObject, IEditorOperation
{
    private string _title;
    private int _current;
    private int _total;

    /// <summary>
    /// Создаёт операцию с заголовком и (опционально) ожидаемым объёмом работы.
    /// </summary>
    /// <param name="key">Ключ операции для поиска при обновлении прогресса (например, имя модуля).</param>
    /// <param name="title">Заголовок операции для отображения.</param>
    /// <param name="total">Ожидаемое общее число единиц работы; 0 — прогресс неизвестен.</param>
    public EditorOperation(string key, string title, int total)
    {
        Key = key;
        _title = title ?? string.Empty;
        _total = Math.Max(total, 0);
        RefCount = 1;
    }

    // todo: для этого специально есть GUID вроде бы.
    /// <summary>
    /// Ключ операции; используется для дедупликации и адресного обновления прогресса.
    /// </summary>
    internal string Key { get; }

    // todo: Точно ли необходимая вещь? 
    /// <summary>
    /// Число активных удержаний операции (Begin увеличивает, End уменьшает).
    /// </summary>
    internal int RefCount { get; set; }

    /// <inheritdoc />
    public string Title
    {
        get => _title;
        private set
        {
            if (_title == value)
            {
                return;
            }

            _title = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <inheritdoc />
    public int Current => _current;

    /// <inheritdoc />
    public int Total => _total;

    // todo: Мб вообще лишнее в контексте приложения. Хотя мб и полезная штука для расширений. Но для этого нужно какую-то другую логику сдлеать для этого.
    /// <inheritdoc />
    public bool IsIndeterminate => _total <= 0;

    /// <inheritdoc />
    public double Fraction => _total > 0 ? Math.Clamp((double)_current / _total, 0, 1) : 0;

    // todo: локализация
    /// <inheritdoc />
    public string DisplayText => _total > 0 ? $"{_title}: {_current} из {_total}" : _title;

    // todo: если оно вызывается на ui потоке, и должно отправлять важную информацию, то почему эта самая информация отправляется сюда? Так быть не должно. Странный метод.
    /// <summary>
    /// Обновляет прогресс операции; вызывается на UI-потоке трекером.
    /// </summary>
    /// <param name="current">Число выполненных единиц работы.</param>
    /// <param name="total">Общее число единиц работы; неположительное значение оставляет прежнее.</param>
    internal void Report(int current, int total)
    {
        if (total > 0)
        {
            _total = total;
        }

        _current = Math.Max(current, 0);
        OnPropertyChanged(nameof(Current));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(IsIndeterminate));
        OnPropertyChanged(nameof(Fraction));
        OnPropertyChanged(nameof(DisplayText));
    }

    // todo: а надо ли оно если можно просто новую операцию запустить? Словно смысла нет. Это вообще неправильно так-то.
    /// <summary>
    /// Меняет заголовок операции (например, при повторном Begin с тем же ключом).
    /// </summary>
    /// <param name="title">Новый заголовок.</param>
    internal void Retitle(string title) => Title = title;
}