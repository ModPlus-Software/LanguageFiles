namespace LangFilesEditor.UI.Infrastructure.Docking;

using System.Windows;
using Utils;

/// <summary>
/// Управляет закреплением, откреплением и видимостью панелей на сайте докинга.
/// </summary>
public sealed class DockManager
{
    private readonly DockSite _site;
    private readonly Dictionary<DockablePanel, FloatWindow> _floatWindows = new();
    
    /// <summary>
    /// Создаёт менеджер докинга для указанного сайта.
    /// </summary>
    /// <param name="site">Сайт докинга с хостами для каждой стороны.</param>
    public DockManager(DockSite site) => _site = site;
    
    /// <summary>
    /// Регистрирует панель и закрепляет её на стороне по умолчанию.
    /// </summary>
    /// <param name="panel">Закрепляемая панель.</param>
    public void Register(DockablePanel panel) => Dock(panel, panel.DockSide);
    
    /// <summary>
    /// Закрепляет панель на указанной стороне, закрывая плавающее окно при необходимости.
    /// </summary>
    /// <param name="panel">Панель для закрепления.</param>
    /// <param name="side">Сторона докинга.</param>
    public void Dock(DockablePanel panel, DockSide side)
    {
        if (_floatWindows.TryGetValue(panel, out FloatWindow floatWindow))
        {
            floatWindow.PanelClosing -= OnFloatWindowPanelClosing;
            var content = floatWindow.ExtractContent();
            floatWindow.Close();
            _floatWindows.Remove(panel);
            
            if (content != null)
            {
                panel.RestoreContent(content);
            }
        }
        
        panel.DockSide = side;
        panel.State.DockSide = side;
        panel.State.IsFloating = false;
        var host = _site.GetHost(side);
        
        if (!ReferenceEquals(host.Content, panel))
        {
            WpfUtils.ReparentToContentControl(panel, host);
        }
        
        _site.UpdateLayoutMetrics();
    }
    
    /// <summary>
    /// Открепляет панель в отдельное плавающее окно.
    /// </summary>
    /// <param name="panel">Панель для открепления.</param>
    /// <param name="screenPosition">Экранная позиция окна; null — позиция по умолчанию.</param>
    public void Float(DockablePanel panel, Point? screenPosition = null)
    {
        if (_floatWindows.ContainsKey(panel))
        {
            return;
        }
        
        var content = panel.ExtractContent();
        if (content is null)
        {
            return;
        }
        
        _site.RememberPanelSize(panel);
        panel.State.IsFloating = true;
        var owner = Window.GetWindow(_site);
        var floatWindow = new FloatWindow(content, panel, owner);
        if (screenPosition is { } p)
        {
            floatWindow.Left = p.X - 24;
            floatWindow.Top = p.Y - 16;
        }
        
        floatWindow.PanelClosing += OnFloatWindowPanelClosing;
        floatWindow.Show();
        _floatWindows[panel] = floatWindow;
        _site.UpdateLayoutMetrics();
    }
    
    /// <summary>
    /// Показывает панель, закрепляя её при необходимости.
    /// </summary>
    /// <param name="panel">Панель для показа.</param>
    public void ShowPanel(DockablePanel panel)
    {
        panel.State.Show();
        if (_floatWindows.ContainsKey(panel))
        {
            Dock(panel, panel.DockSide);
        }
        else
        {
            var host = _site.GetHost(panel.DockSide);
            if (!ReferenceEquals(host.Content, panel))
            {
                Dock(panel, panel.DockSide);
            }
        }
        
        _site.UpdateLayoutMetrics();
    }
    
    /// <summary>
    /// Скрывает панель, сохраняя её состояние докинга.
    /// </summary>
    /// <param name="panel">Панель для скрытия.</param>
    public void HidePanel(DockablePanel panel)
    {
        if (_floatWindows.ContainsKey(panel))
        {
            Dock(panel, panel.State.DockSide);
        }
        
        panel.State.Close();
        _site.UpdateLayoutMetrics();
    }
    
    /// <summary>
    /// Возвращает сайт докинга, управляемый этим менеджером.
    /// </summary>
    /// <returns>Сайт докинга.</returns>
    public DockSite GetSite() => _site;
    
    private void OnFloatWindowPanelClosing(DockablePanel panel) => Dock(panel, panel.State.DockSide);
}