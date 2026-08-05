namespace LangFilesEditor.UI.Windows.MainWindow;

using Infrastructure.Docking;

/// <summary>
/// Панель команд редактора (разметка в XAML, логика в <see cref="ToolBarVM"/>).
/// </summary>
public partial class ToolBar
{
    /// <summary>
    /// Ширина одной кнопки панели инструментов.
    /// </summary>
    public const double ButtonWidth = 128;

    /// <summary>
    /// Высота одной кнопки панели инструментов.
    /// </summary>
    public const double ButtonHeight = 28;

    /// <summary>
    /// Внутренний отступ содержимого панели вокруг кнопок.
    /// </summary>
    public const double ContentPadding = 20;

    /// <summary>
    /// Ширина содержимого панели: кнопка + отступ.
    /// </summary>
    public const double ContentWidth = ButtonWidth + ContentPadding;

    /// <summary>
    /// Толщина рамки закреплённой панели (см. <c>DockPanelChromeBorderStyle</c>).
    /// </summary>
    public const double BorderWidth = 2;

    /// <summary>
    /// Итоговая ширина закреплённой справа панели инструментов: содержимое + хром докинга + рамка.
    /// Используется при создании <see cref="MainWindowVM"/>, так как на момент её конструирования
    /// самого view с этими ресурсами ещё не существует.
    /// </summary>
    public const double DockPanelWidth = ContentWidth + DockChromeMetrics.VerticalWidth + BorderWidth;

    /// <summary>
    /// Инициализирует компоненты панели инструментов.
    /// </summary>
    public ToolBar()
    {
        InitializeComponent();
    }
}