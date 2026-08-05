namespace LangFilesEditor.UI.Windows.MainWindow;

/// <summary>
/// Тип сегмента в строке состояния.
/// </summary>
public enum StatusBarSegmentKind
{
    /// <summary>
    /// Текстовая метка (домен, модуль, запись).
    /// </summary>
    Label,

    /// <summary>
    /// Стрелка-разделитель иерархии.
    /// </summary>
    Arrow,

    /// <summary>
    /// Вертикальный разделитель сегментов.
    /// </summary>
    Separator
}

/// <summary>
/// Модель одного сегмента строки состояния для привязки в UI.
/// </summary>
public sealed class StatusBarSegmentVm
{
    /// <summary>
    /// Тип отображаемого сегмента.
    /// </summary>
    public StatusBarSegmentKind Kind { get; init; }

    /// <summary>
    /// Текст сегмента.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Подсказка сегмента.
    /// </summary>
    public string ToolTip { get; init; } = string.Empty;

    /// <summary>
    /// Максимальная ширина метки; для <see cref="StatusBarSegmentKind.Label"/>; null — автоширина.
    /// </summary>
    public double? MaxWidth { get; init; }

    /// <summary>
    /// Ширина метки для WPF; без ограничения — большое значение.
    /// </summary>
    public double LabelMaxWidth => MaxWidth ?? 4096;
}