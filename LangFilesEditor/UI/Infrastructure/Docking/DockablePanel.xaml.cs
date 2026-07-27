namespace LangFilesEditor.UI.Infrastructure.Docking;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

/// <summary>
/// Закрепляемая панель с хромом, перетаскиванием и переключением между докингом и плавающим окном.
/// </summary>
public partial class DockablePanel
{
    private Point _dragStartScreen;
    private bool _isDragging;
    private DockManager _dockManager;
    private int _contentRowIndex = 1;
    private int _contentColumnIndex;
    
    /// <summary>
    /// DependencyProperty для <see cref="State"/>.
    /// </summary>
    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(
            nameof(State),
            typeof(DockPanelState),
            typeof(DockablePanel),
            new PropertyMetadata(null, OnStateChanged));
    
    /// <summary>
    /// DependencyProperty для <see cref="PanelContent"/>.
    /// </summary>
    public static readonly DependencyProperty PanelContentProperty =
        DependencyProperty.Register(
            nameof(PanelContent),
            typeof(object),
            typeof(DockablePanel),
            new PropertyMetadata(null));
    
    /// <summary>
    /// DependencyProperty для <see cref="DockSide"/>.
    /// </summary>
    public static readonly DependencyProperty DockSideProperty =
        DependencyProperty.Register(
            nameof(DockSide),
            typeof(DockSide),
            typeof(DockablePanel),
            new PropertyMetadata(DockSide.Left, OnDockSideChanged));
    
    /// <summary>
    /// DependencyProperty для <see cref="ChromePlacement"/>.
    /// </summary>
    public static readonly DependencyProperty ChromePlacementProperty =
        DependencyProperty.Register(
            nameof(ChromePlacement),
            typeof(DockChromePlacement),
            typeof(DockablePanel),
            new PropertyMetadata(DockChromePlacement.Top, OnChromePlacementChanged));
    
    /// <summary>
    /// Инициализирует компонент и подписывается на событие загрузки.
    /// </summary>
    public DockablePanel()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }
    
    /// <summary>
    /// Состояние панели: видимость, размеры, сторона докинга и режим плавания.
    /// </summary>
    public DockPanelState State
    {
        get => (DockPanelState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }
    
    /// <summary>
    /// Содержимое панели (обычно другой <see cref="UserControl"/>).
    /// </summary>
    public object PanelContent
    {
        get => GetValue(PanelContentProperty);
        set => SetValue(PanelContentProperty, value);
    }
    
    /// <summary>
    /// Сторона докинга относительно центральной области.
    /// </summary>
    public DockSide DockSide
    {
        get => (DockSide)GetValue(DockSideProperty);
        set => SetValue(DockSideProperty, value);
    }
    
    /// <summary>
    /// Расположение хрома (заголовок и кнопки) относительно содержимого.
    /// </summary>
    public DockChromePlacement ChromePlacement
    {
        get => (DockChromePlacement)GetValue(ChromePlacementProperty);
        set => SetValue(ChromePlacementProperty, value);
    }
    
    /// <summary>
    /// <see langword="true"/>, если содержимое панели задано.
    /// </summary>
    public bool HasPanelContent => PanelContent != null;
    
    internal void AttachManager(DockManager manager) => _dockManager = manager;
    
    /// <summary>
    /// Извлекает содержимое из панели для переноса в плавающее окно или другой хост.
    /// </summary>
    /// <returns>Извлечённый элемент или <see langword="null"/>, если содержимое не является <see cref="FrameworkElement"/>.</returns>
    public FrameworkElement ExtractContent()
    {
        if (PanelContent is not FrameworkElement content)
        {
            return null;
        }
        
        PanelContent = null;
        Utils.WpfUtils.Detach(content);
        ApplyState(State);
        return content;
    }
    
    /// <summary>
    /// Возвращает содержимое в панель после закрытия плавающего окна или смены хоста.
    /// </summary>
    /// <param name="content">Элемент, ранее извлечённый через <see cref="ExtractContent"/>.</param>
    public void RestoreContent(FrameworkElement content)
    {
        PanelContent = content;
        ApplyState(State);
    }
    
    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DockablePanel panel)
        {
            return;
        }
        
        if (e.OldValue is DockPanelState oldState)
        {
            oldState.PropertyChanged -= panel.OnStatePropertyChanged;
        }
        
        if (e.NewValue is DockPanelState newState)
        {
            newState.PropertyChanged += panel.OnStatePropertyChanged;
            panel.ChromePlacement = newState.ChromePlacement;
            panel.ApplyState(newState);
        }
    }
    
    private static void OnDockSideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockablePanel { State: null } panel)
        {
            panel.ChromePlacement = MapChromePlacement((DockSide)e.NewValue);
        }
    }
    
    private static void OnChromePlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockablePanel panel)
        {
            panel.ApplyChromeLayout();
        }
    }
    
    private static DockChromePlacement MapChromePlacement(DockSide side) => side switch
    {
        DockSide.Left => DockChromePlacement.Left,
        DockSide.Right => DockChromePlacement.Right,
        _ => DockChromePlacement.Top
    };
    
    private void OnStatePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        ApplyState(State);
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (State != null)
        {
            ChromePlacement = State.ChromePlacement;
        }
        
        ApplyChromeLayout();
        ApplyState(State);
    }
    
    private void ApplyChromeLayout()
    {
        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();
        HeaderLayout.RowDefinitions.Clear();
        HeaderLayout.ColumnDefinitions.Clear();
        HeaderLayout.Children.Clear();
        TitleText.LayoutTransform = null;
        TitleText.Margin = new Thickness(0);
        HeaderButtons.Orientation = Orientation.Horizontal;
        switch (ChromePlacement)
        {
            case DockChromePlacement.Left:
                ApplyLeftChromeLayout();
                break;
            case DockChromePlacement.Right:
                ApplyRightChromeLayout();
                break;
            default:
                ApplyTopChromeLayout();
                break;
        }
        
        UpdateCollapseGlyph();
    }
    
    private void ApplyTopChromeLayout()
    {
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(HeaderBorder, 0);
        Grid.SetColumn(HeaderBorder, 0);
        Grid.SetRow(ContentHost, 1);
        Grid.SetColumn(ContentHost, 0);
        _contentRowIndex = 1;
        _contentColumnIndex = 0;
        HeaderBorder.BorderThickness = new Thickness(0, 0, 0, 1);
        HeaderBorder.Padding = new Thickness(4, 2, 4, 2);
        ClearVerticalChromeSize();
        HeaderBorder.MinHeight = DockChromeMetrics.HorizontalHeight;
        HeaderBorder.MaxHeight = DockChromeMetrics.HorizontalHeight;
        HeaderLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        HeaderLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        HeaderLayout.Children.Add(TitleText);
        HeaderLayout.Children.Add(HeaderButtons);
        Grid.SetColumn(TitleText, 0);
        Grid.SetColumn(HeaderButtons, 1);
        TitleText.HorizontalAlignment = HorizontalAlignment.Left;
        TitleText.VerticalAlignment = VerticalAlignment.Center;
        TitleText.TextTrimming = TextTrimming.CharacterEllipsis;
        TitleText.TextWrapping = TextWrapping.NoWrap;
    }
    
    private void ApplyLeftChromeLayout() => ApplyVerticalChromeLayout(isRight: false);
    
    private void ApplyRightChromeLayout() => ApplyVerticalChromeLayout(isRight: true);
    
    private void ApplyVerticalChromeLayout(bool isRight)
    {
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        if (isRight)
        {
            RootGrid.ColumnDefinitions.Clear();
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(ContentHost, 0);
            Grid.SetRow(ContentHost, 0);
            Grid.SetColumn(HeaderBorder, 1);
            Grid.SetRow(HeaderBorder, 0);
            _contentColumnIndex = 0;
            HeaderBorder.BorderThickness = new Thickness(1, 0, 0, 0);
            TitleText.LayoutTransform = new RotateTransform(90);
        }
        else
        {
            Grid.SetColumn(HeaderBorder, 0);
            Grid.SetRow(HeaderBorder, 0);
            Grid.SetColumn(ContentHost, 1);
            Grid.SetRow(ContentHost, 0);
            _contentColumnIndex = 1;
            HeaderBorder.BorderThickness = new Thickness(0, 0, 1, 0);
            TitleText.LayoutTransform = new RotateTransform(-90);
        }
        
        _contentRowIndex = 0;
        ApplyVerticalChromeSize();
        HeaderBorder.Padding = new Thickness(1, 2, 1, 2);
        HeaderLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        HeaderLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        HeaderLayout.Children.Add(HeaderButtons);
        HeaderLayout.Children.Add(TitleText);
        Grid.SetRow(HeaderButtons, 0);
        Grid.SetRow(TitleText, 1);
        HeaderButtons.Orientation = Orientation.Vertical;
        HeaderButtons.VerticalAlignment = VerticalAlignment.Top;
        HeaderButtons.HorizontalAlignment = HorizontalAlignment.Center;
        TitleText.Margin = new Thickness(0, 2, 0, 0);
        TitleText.HorizontalAlignment = HorizontalAlignment.Center;
        TitleText.VerticalAlignment = VerticalAlignment.Top;
        TitleText.TextTrimming = TextTrimming.None;
        TitleText.TextWrapping = TextWrapping.NoWrap;
        ApplyVerticalHeaderWidth(State);
    }
    
    private void ApplyVerticalChromeSize()
    {
        HeaderBorder.MinWidth = DockChromeMetrics.VerticalWidth;
        HeaderBorder.ClearValue(WidthProperty);
        HeaderBorder.ClearValue(MaxWidthProperty);
        HeaderBorder.MinHeight = 0;
        HeaderBorder.MaxHeight = double.PositiveInfinity;
        HeaderBorder.ClearValue(HeightProperty);
    }
    
    private void ApplyVerticalHeaderWidth(DockPanelState state)
    {
        if (ChromePlacement is not (DockChromePlacement.Left or DockChromePlacement.Right))
        {
            return;
        }
        
        if (state?.IsCollapsed == true)
        {
            HeaderBorder.Width = DockChromeMetrics.VerticalWidth;
            HeaderBorder.MaxWidth = DockChromeMetrics.VerticalWidth;
            return;
        }
        
        HeaderBorder.ClearValue(WidthProperty);
        HeaderBorder.ClearValue(MaxWidthProperty);
    }
    
    private void ClearVerticalChromeSize()
    {
        HeaderBorder.ClearValue(WidthProperty);
        HeaderBorder.ClearValue(MinWidthProperty);
        HeaderBorder.ClearValue(MaxWidthProperty);
        HeaderBorder.ClearValue(MinHeightProperty);
        HeaderBorder.ClearValue(MaxHeightProperty);
        HeaderBorder.ClearValue(HeightProperty);
    }
    
    private void ApplyState(DockPanelState state)
    {
        if (state == null)
        {
            return;
        }
        
        Visibility = state.IsVisible && !state.IsFloating ? Visibility.Visible : Visibility.Collapsed;
        var showContent = HasPanelContent && !state.IsCollapsed;
        ContentHost.Visibility = showContent ? Visibility.Visible : Visibility.Collapsed;
        if (ChromePlacement == DockChromePlacement.Top)
        {
            if (RootGrid.RowDefinitions.Count > _contentRowIndex)
            {
                RootGrid.RowDefinitions[_contentRowIndex].Height = showContent
                ? new GridLength(1, GridUnitType.Star)
                : new GridLength(0);
            }
        }
        else if (RootGrid.ColumnDefinitions.Count > _contentColumnIndex)
        {
            RootGrid.ColumnDefinitions[_contentColumnIndex].Width = showContent
                ? new GridLength(1, GridUnitType.Star)
                : new GridLength(0);
        }
        
        var showTitle = ChromePlacement == DockChromePlacement.Top || state.IsCollapsed == false;
        TitleText.Visibility = showTitle ? Visibility.Visible : Visibility.Collapsed;
        ApplyVerticalHeaderWidth(state);
        UpdateCollapseGlyph();
    }
    
    private void UpdateCollapseGlyph()
    {
        var expanded = State?.IsCollapsed != true;
        CollapseGlyph.Text = ChromePlacement switch
        {
            DockChromePlacement.Left => expanded ? "\uE76B" : "\uE76C",
            DockChromePlacement.Right => expanded ? "\uE76C" : "\uE76B",
            _ => expanded ? "\uE70D" : "\uE70E"
        };
    }
    
    private void OnCollapseClick(object sender, RoutedEventArgs e)
    {
        if (State == null)
        {
            return;
        }
        State.ToggleCollapsed();
        ApplyState(State);
        _dockManager?.GetSite().UpdateLayoutMetrics();
    }
    
    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (State == null)
        {
            return;
        }
        
        if (State.IsFloating)
        {
            _dockManager?.Dock(this, State.DockSide);
        }
        
        State.Close();
        _dockManager?.GetSite().UpdateLayoutMetrics();
    }
    
    private void OnFloatClick(object sender, RoutedEventArgs e) => _dockManager?.Float(this);
    
    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartScreen = PointToScreen(e.GetPosition(this));
        _isDragging = false;
        HeaderBorder.CaptureMouse();
    }
    
    private void OnHeaderMouseMove(object sender, MouseEventArgs e)
    {
        if (!HeaderBorder.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed || _dockManager == null)
        {
            return;
        }
        
        var current = PointToScreen(e.GetPosition(this));
        if (!_isDragging && (current - _dragStartScreen).Length > 6)
        {
            _isDragging = true;
        }
        
        if (!_isDragging)
        {
            return;
        }
        
        var dockSite = _dockManager.GetSite();
        var targetSide = dockSite.HitTestDockSide(current);
        dockSite.ShowDropHint(targetSide);
        if (!dockSite.IsPointInside(current))
        {
            _dockManager.Float(this, current);
        }
    }
    private void OnHeaderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!HeaderBorder.IsMouseCaptured)
        {
            return;
        }
        HeaderBorder.ReleaseMouseCapture();
        _dockManager?.GetSite().ClearDropHint();
        _isDragging = false;
    }
}