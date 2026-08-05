namespace LangFilesEditor.Utils;

/// <summary>
/// Размер порции строк, добавляемых в интерфейс между уступками кадра UI-потоку.
/// <para>
/// Отдавать кадр после каждой строки — плавно, но дорого: строки грида не виртуализируются,
/// и каждый кадр стоит прохода разметки по всему списку. Отдавать кадр по таймеру нельзя:
/// добавление строки в коллекцию почти бесплатно, разметка и отрисовка происходят уже во время
/// уступки, поэтому за отведённое время набирается порция, которая потом рисуется одним рывком.
/// </para>
/// <para>
/// Поэтому порция считается в строках и растёт от <see cref="InitialRowsPerFrame"/> до
/// <see cref="MaxRowsPerFrame"/>: первые кадры мелкие — видимая часть таблицы наполняется плавно,
/// дальше порции крупнее — то, что уже за пределами экрана, дорисовывается быстро.
/// </para>
/// </summary>
internal sealed class UiYieldBudget
{
    /// <summary>
    /// Размер первой порции строк.
    /// </summary>
    private const int InitialRowsPerFrame = 4;

    /// <summary>
    /// Предельный размер порции строк.
    /// </summary>
    private const int MaxRowsPerFrame = 32;

    private int _rowsPerFrame = InitialRowsPerFrame;
    private int _rowsSinceYield;

    /// <summary>
    /// Учитывает добавленную строку.
    /// </summary>
    /// <returns><see langword="true"/>, если порция набрана и пора отдать кадр UI-потоку.</returns>
    public bool RegisterRow() => ++_rowsSinceYield >= _rowsPerFrame;

    /// <summary>
    /// Отмечает отданный кадр и увеличивает размер следующей порции.
    /// </summary>
    public void Reset()
    {
        _rowsSinceYield = 0;
        _rowsPerFrame = Math.Min(_rowsPerFrame * 2, MaxRowsPerFrame);
    }
}
