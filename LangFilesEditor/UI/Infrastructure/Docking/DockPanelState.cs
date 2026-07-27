namespace LangFilesEditor.UI.Infrastructure.Docking;

using ModPlusAPI.Mvvm;

/// <summary>
/// Состояние закрепляемой панели: видимость, размеры, сторона докинга и режим плавания.
/// </summary>
public class DockPanelState : ObservableObject
{
    private bool _isVisible = true;
    private bool _isCollapsed;
    private bool _isFloating;
    private DockSide _dockSide;
    private double _dockedWidth = double.NaN;
    private double _dockedHeight = double.NaN;
    private double _floatingWidth = double.NaN;
    private double _floatingHeight = double.NaN;
    
    /// <summary>
    /// Создаёт состояние панели с параметрами по умолчанию.
    /// </summary>
    /// <param name="title">Заголовок панели.</param>
    /// <param name="defaultSide">Сторона докинга по умолчанию.</param>
    /// <param name="defaultWidth">Ширина по умолчанию для боковых панелей; NaN — значение по умолчанию системы.</param>
    /// <param name="defaultHeight">Высота по умолчанию для верхних/нижних панелей; NaN — значение по умолчанию системы.</param>
    /// <param name="chromePlacement">Расположение хрома; null — определяется по стороне докинга.</param>
    /// <param name="minDockedSpan">Минимальный размер панели в закреплённом режиме; NaN — без ограничения.</param>
    /// <param name="maxDockedSpan">Максимальный размер панели в закреплённом режиме; NaN — без ограничения.</param>
    public DockPanelState(
        string title,
        DockSide defaultSide,
        double defaultWidth = double.NaN,
        double defaultHeight = double.NaN,
        DockChromePlacement? chromePlacement = null,
        double minDockedSpan = double.NaN,
        double maxDockedSpan = double.NaN)
    {
        Title = title;
        _dockSide = defaultSide;
        DefaultWidth = defaultWidth;
        DefaultHeight = defaultHeight;
        ChromePlacement = chromePlacement ?? MapChromePlacement(defaultSide);
        MinDockedSpan = minDockedSpan;
        MaxDockedSpan = maxDockedSpan;
    }
    
    /// <summary>
    /// Заголовок панели.
    /// </summary>
    public string Title { get; }
    
    /// <summary>
    /// Расположение хрома панели.
    /// </summary>
    public DockChromePlacement ChromePlacement { get; }
    
    /// <summary>
    /// Размер заголовка в свёрнутом состоянии (ширина или высота в зависимости от хрома).
    /// </summary>
    public double CollapsedHeaderSpan =>
        ChromePlacement == DockChromePlacement.Top
            ? DockChromeMetrics.HorizontalHeight
            : DockChromeMetrics.VerticalWidth;
    
    /// <summary>
    /// Текущая сторона докинга панели.
    /// </summary>
    public DockSide DockSide
    {
        get => _dockSide;
        set
        {
            if (_dockSide == value)
            {
                return;
            }
            
            _dockSide = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Ширина панели по умолчанию в закреплённом режиме.
    /// </summary>
    public double DefaultWidth { get; }
    
    /// <summary>
    /// Высота панели по умолчанию в закреплённом режиме.
    /// </summary>
    public double DefaultHeight { get; }
    
    /// <summary>
    /// Минимальный размер панели в закреплённом режиме.
    /// </summary>
    public double MinDockedSpan { get; }
    
    /// <summary>
    /// Максимальный размер панели в закреплённом режиме.
    /// </summary>
    public double MaxDockedSpan { get; }
    
    /// <summary>
    /// Видима ли панель.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }
            
            _isVisible = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Свёрнута ли панель (отображается только заголовок).
    /// </summary>
    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (_isCollapsed == value)
            {
                return;
            }
            
            _isCollapsed = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Находится ли панель в плавающем окне.
    /// </summary>
    public bool IsFloating
    {
        get => _isFloating;
        set
        {
            if (_isFloating == value)
            {
                return;
            }
            
            _isFloating = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Текущая ширина панели в закреплённом режиме.
    /// </summary>
    public double DockedWidth
    {
        get => _dockedWidth;
        set => _dockedWidth = ClampDockedSpan(value);
    }
    
    /// <summary>
    /// Текущая высота панели в закреплённом режиме.
    /// </summary>
    public double DockedHeight
    {
        get => _dockedHeight;
        set => _dockedHeight = ClampDockedSpan(value);
    }
    
    /// <summary>
    /// Ширина плавающего окна панели.
    /// </summary>
    public double FloatingWidth
    {
        get => _floatingWidth;
        set => _floatingWidth = value;
    }
    
    /// <summary>
    /// Высота плавающего окна панели.
    /// </summary>
    public double FloatingHeight
    {
        get => _floatingHeight;
        set => _floatingHeight = value;
    }
    
    /// <summary>
    /// Возвращает размер панели в закреплённом режиме для указанной стороны.
    /// </summary>
    /// <param name="side">Сторона докинга.</param>
    /// <returns>Ширина или высота с учётом ограничений и значений по умолчанию.</returns>
    public double GetDockedSpan(DockSide side)
    {
        var span = side is DockSide.Left or DockSide.Right
            ? double.IsNaN(_dockedWidth) ? (double.IsNaN(DefaultWidth) ? 300 : DefaultWidth) : _dockedWidth
            : double.IsNaN(_dockedHeight) ? (double.IsNaN(DefaultHeight) ? 96 : DefaultHeight) : _dockedHeight;
        
        return ClampDockedSpan(span);
    }
    
    private double ClampDockedSpan(double span)
    {
        if (!double.IsNaN(MinDockedSpan))
        {
            span = Math.Max(span, MinDockedSpan);
        }
        
        if (!double.IsNaN(MaxDockedSpan))
        {
            span = Math.Min(span, MaxDockedSpan);
        }
        
        return span;
    }
    
    /// <summary>
    /// Возвращает размеры плавающего окна для указанной стороны докинга.
    /// </summary>
    /// <param name="side">Сторона докинга.</param>
    /// <returns>Кортеж (ширина, высота) плавающего окна.</returns>
    public (double Width, double Height) GetFloatingSize(DockSide side)
    {
        var width = !double.IsNaN(_floatingWidth)
            ? _floatingWidth : side is DockSide.Left or DockSide.Right ? GetDockedSpan(side) : 480;
        
        var height = !double.IsNaN(_floatingHeight)
            ? _floatingHeight : side is DockSide.Top or DockSide.Bottom ? GetDockedSpan(side) : 320;
        
        return (width, height);
    }
    
    /// <summary>
    /// Переключает свёрнутое состояние панели.
    /// </summary>
    public void ToggleCollapsed() => IsCollapsed = !IsCollapsed;
    
    /// <summary>
    /// Скрывает панель.
    /// </summary>
    public void Close() => IsVisible = false;
    
    /// <summary>
    /// Показывает панель.
    /// </summary>
    public void Show() => IsVisible = true;
    
    private static DockChromePlacement MapChromePlacement(DockSide side) => side switch
    {
        DockSide.Left => DockChromePlacement.Left,
        DockSide.Right => DockChromePlacement.Right,
        _ => DockChromePlacement.Top
    };
}