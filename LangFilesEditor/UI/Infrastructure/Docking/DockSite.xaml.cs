namespace LangFilesEditor.UI.Infrastructure.Docking;

using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Контейнер докинга: четыре боковые зоны, центральная рабочая область и подсказки при перетаскивании.
/// </summary>
public partial class DockSite
{
    /// <summary>
    /// DependencyProperty для <see cref="WorkSpaceContent"/>.
    /// </summary>
    public static readonly DependencyProperty WorkSpaceContentProperty =
        DependencyProperty.Register(
            nameof(WorkSpaceContent),
            typeof(object),
            typeof(DockSite),
            new PropertyMetadata(null, OnWorkSpaceContentChanged));

    private readonly List<DockablePanel> _panels = new();

    /// <summary>
    /// Инициализирует разметку сайта и создаёт <see cref="Manager"/>.
    /// </summary>
    public DockSite()
    {
        InitializeComponent();
        Manager = new DockManager(this);
        Loaded += OnLoaded;
    }

    /// <summary>
    /// Менеджер закрепления, открепления и видимости панелей.
    /// </summary>
    public DockManager Manager { get; }

    /// <summary>
    /// Содержимое центральной рабочей области редактора.
    /// </summary>
    public object WorkSpaceContent
    {
        get => GetValue(WorkSpaceContentProperty);
        set => SetValue(WorkSpaceContentProperty, value);
    }

    /// <summary>
    /// Регистрирует панель на сайте докинга и подписывает её на обновление метрик разметки.
    /// </summary>
    /// <param name="panel">Панель с уже заданным <see cref="DockablePanel.State"/>.</param>
    /// <exception cref="InvalidOperationException">Если у панели не задано состояние.</exception>
    public void RegisterPanel(DockablePanel panel)
    {
        if (_panels.Contains(panel))
        {
            return;
        }

        if (panel.State is null)
        {
            throw new InvalidOperationException(
            $"DockablePanel '{panel.Name}' requires State to be set before registration.");
        }

        _panels.Add(panel);
        panel.AttachManager(Manager);
        panel.State.PropertyChanged += (_, _) => UpdateLayoutMetrics();
        Manager.Register(panel);
    }

    /// <summary>
    /// Возвращает хост-контейнер для указанной стороны докинга.
    /// </summary>
    /// <param name="side">Сторона докинга.</param>
    /// <returns>Контейнер, в который помещается закреплённая панель.</returns>
    public ContentControl GetHost(DockSide side) => side switch
    {
        DockSide.Left => LeftHost,
        DockSide.Right => RightHost,
        DockSide.Top => TopHost,
        DockSide.Bottom => BottomHost,
        _ => throw new ArgumentOutOfRangeException(nameof(side))
    };

    /// <summary>
    /// Сохраняет текущий размер закреплённой панели в её состояние после изменения сплиттера.
    /// </summary>
    /// <param name="panel">Панель, размер которой нужно запомнить.</param>
    public void RememberPanelSize(DockablePanel panel)
    {
        switch (panel.DockSide)
        {
            case DockSide.Left when LeftColumn.Width.IsAbsolute:
                panel.State.DockedWidth = LeftColumn.Width.Value;
                break;
            case DockSide.Right when RightColumn.Width.IsAbsolute:
                panel.State.DockedWidth = RightColumn.Width.Value;
                break;
            case DockSide.Top when TopRow.Height.IsAbsolute:
                panel.State.DockedHeight = TopRow.Height.Value;
                break;
            case DockSide.Bottom when BottomRow.Height.IsAbsolute:
                panel.State.DockedHeight = BottomRow.Height.Value;
                break;
        }
    }

    /// <summary>
    /// Пересчитывает ширины и высоты колонок и строк сетки по состоянию всех зарегистрированных панелей.
    /// </summary>
    public void UpdateLayoutMetrics()
    {
        UpdateSide(DockSide.Left, LeftHost, LeftColumn, LeftSplitterColumn, LeftSplitter);
        UpdateSide(DockSide.Right, RightHost, RightColumn, RightSplitterColumn, RightSplitter);
        UpdateSide(DockSide.Top, TopHost, TopRow, TopSplitterRow, TopSplitter);
        UpdateSide(DockSide.Bottom, BottomHost, BottomRow, BottomSplitterRow, BottomSplitter);
    }

    /// <summary>
    /// Проверяет, попадает ли экранная точка внутрь области сайта докинга.
    /// </summary>
    /// <param name="screenPoint">Координаты точки в экранной системе.</param>
    /// <returns><see langword="true"/>, если точка внутри сайта.</returns>
    public bool IsPointInside(Point screenPoint)
    {
        var topLeft = PointToScreen(new Point(0, 0));
        var bottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));
        return screenPoint.X >= topLeft.X && screenPoint.X <= bottomRight.X
               && screenPoint.Y >= topLeft.Y && screenPoint.Y <= bottomRight.Y;
    }

    /// <summary>
    /// Определяет сторону докинга по положению курсора у края сайта (для подсказки при перетаскивании).
    /// </summary>
    /// <param name="screenPoint">Координаты курсора в экранной системе.</param>
    /// <returns>Сторона докинга или <see langword="null"/>, если курсор не у края.</returns>
    public DockSide? HitTestDockSide(Point screenPoint)
    {
        const double edge = 48;

        // Обе границы берутся через PointToScreen: ActualWidth/ActualHeight заданы в аппаратно-независимых
        // единицах и при масштабе экрана ≠ 100% не совпадают с экранными пикселями.
        var topLeft = PointToScreen(new Point(0, 0));
        var bottomRight = PointToScreen(new Point(ActualWidth, ActualHeight));

        if (screenPoint.X <= topLeft.X + edge)
        {
            return DockSide.Left;
        }

        if (screenPoint.X >= bottomRight.X - edge)
        {
            return DockSide.Right;
        }

        if (screenPoint.Y <= topLeft.Y + edge)
        {
            return DockSide.Top;
        }

        if (screenPoint.Y >= bottomRight.Y - edge)
        {
            return DockSide.Bottom;
        }

        return null;
    }

    /// <summary>
    /// Показывает визуальную подсказку зоны сброса на указанной стороне.
    /// </summary>
    /// <param name="side">Сторона докинга или <see langword="null"/>, чтобы скрыть все подсказки.</param>
    public void ShowDropHint(DockSide? side)
    {
        LeftDropHint.Visibility = side == DockSide.Left ? Visibility.Visible : Visibility.Collapsed;
        RightDropHint.Visibility = side == DockSide.Right ? Visibility.Visible : Visibility.Collapsed;
        TopDropHint.Visibility = side == DockSide.Top ? Visibility.Visible : Visibility.Collapsed;
        BottomDropHint.Visibility = side == DockSide.Bottom ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Скрывает все подсказки зон сброса при перетаскивании панели.
    /// </summary>
    public void ClearDropHint()
    {
        LeftDropHint.Visibility = Visibility.Collapsed;
        RightDropHint.Visibility = Visibility.Collapsed;
        TopDropHint.Visibility = Visibility.Collapsed;
        BottomDropHint.Visibility = Visibility.Collapsed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (WorkSpaceContent != null)
        {
            CenterHost.Content = WorkSpaceContent;
        }

        LeftSplitter.DragCompleted += (_, _) => RememberHostSize(LeftHost);
        RightSplitter.DragCompleted += (_, _) => RememberHostSize(RightHost);
        TopSplitter.DragCompleted += (_, _) => RememberHostSize(TopHost);
        BottomSplitter.DragCompleted += (_, _) => RememberHostSize(BottomHost);
        UpdateLayoutMetrics();
    }

    private void RememberHostSize(ContentControl host)
    {
        if (host.Content is DockablePanel panel)
        {
            RememberPanelSize(panel);
        }
    }

    private static void OnWorkSpaceContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockSite { IsLoaded: true } site)
        {
            site.CenterHost.Content = e.NewValue;
        }
    }

    private void UpdateSide(
        DockSide side,
        ContentControl host,
        DefinitionBase sizeDefinition,
        DefinitionBase splitterDefinition,
        UIElement splitter)
    {
        var panel = host.Content as DockablePanel;
        var visible = panel?.State.IsVisible == true && panel.State.IsFloating == false;

        if (!visible)
        {
            ClearSpanConstraints(sizeDefinition);
            SetSpan(sizeDefinition, new GridLength(0));
            SetSpan(splitterDefinition, new GridLength(0));
            splitter.Visibility = Visibility.Collapsed;
            return;
        }

        if (panel!.State.IsCollapsed)
        {
            SetSpan(sizeDefinition, new GridLength(panel.State.CollapsedHeaderSpan, GridUnitType.Pixel));
            SetSpan(splitterDefinition, new GridLength(0));
            splitter.Visibility = Visibility.Collapsed;
            return;
        }

        var span = panel.State.GetDockedSpan(side);
        ApplySpanConstraints(sizeDefinition, panel.State, side);
        SetSpan(sizeDefinition, new GridLength(span, GridUnitType.Pixel));
        SetSpan(splitterDefinition, new GridLength(4));
        splitter.Visibility = Visibility.Visible;
    }

    private static void ApplySpanConstraints(DefinitionBase sizeDefinition, DockPanelState state, DockSide side)
    {
        switch (sizeDefinition)
        {
            case ColumnDefinition column when side is DockSide.Left or DockSide.Right:
                if (!double.IsNaN(state.MinDockedSpan))
                {
                    column.MinWidth = state.MinDockedSpan;
                }
                else
                {
                    column.ClearValue(ColumnDefinition.MinWidthProperty);
                }

                if (!double.IsNaN(state.MaxDockedSpan))
                {
                    column.MaxWidth = state.MaxDockedSpan;
                }
                else
                {
                    column.ClearValue(ColumnDefinition.MaxWidthProperty);
                }

                break;
            case RowDefinition row when side is DockSide.Top or DockSide.Bottom:
                if (!double.IsNaN(state.MinDockedSpan))
                {
                    row.MinHeight = state.MinDockedSpan;
                }
                else
                {
                    row.ClearValue(RowDefinition.MinHeightProperty);
                }

                if (!double.IsNaN(state.MaxDockedSpan))
                {
                    row.MaxHeight = state.MaxDockedSpan;
                }
                else
                {
                    row.ClearValue(RowDefinition.MaxHeightProperty);
                }

                break;
        }
    }

    private static void ClearSpanConstraints(DefinitionBase sizeDefinition)
    {
        switch (sizeDefinition)
        {
            case ColumnDefinition column:
                column.ClearValue(ColumnDefinition.MinWidthProperty);
                column.ClearValue(ColumnDefinition.MaxWidthProperty);
                break;
            case RowDefinition row:
                row.ClearValue(RowDefinition.MinHeightProperty);
                row.ClearValue(RowDefinition.MaxHeightProperty);
                break;
        }
    }

    private static void SetSpan(DefinitionBase definition, GridLength length)
    {
        switch (definition)
        {
            case ColumnDefinition column:
                column.Width = length;
                break;
            case RowDefinition row:
                row.Height = length;
                break;
        }
    }
}