namespace LangFilesEditor.UI.Infrastructure.Docking;

using System.Windows;

/// <summary>
/// Плавающее окно для откреплённой панели; сохраняет размеры при закрытии.
/// </summary>
public partial class FloatWindow
{
    private readonly DockablePanel _ownerPanel;

    /// <summary>
    /// Создаёт плавающее окно с содержимым панели и размерами из её состояния.
    /// </summary>
    /// <param name="content">Содержимое панели, извлечённое через <see cref="DockablePanel.ExtractContent"/>.</param>
    /// <param name="ownerPanel">Исходная закрепляемая панель.</param>
    /// <param name="owner">Родительское окно приложения.</param>
    public FloatWindow(FrameworkElement content, DockablePanel ownerPanel, Window owner)
    {
        InitializeComponent();
        _ownerPanel = ownerPanel;
        var state = ownerPanel.State;
        Owner = owner;
        Title = state.Title;
        DataContext = owner?.DataContext;
        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Stretch;
        PanelHost.Content = content;
        var (width, height) = state.GetFloatingSize(ownerPanel.DockSide);
        Width = width;
        Height = height;
        if (ownerPanel.DockSide is DockSide.Left or DockSide.Right)
        {
            MinWidth = !double.IsNaN(state.MinDockedSpan) ? state.MinDockedSpan : 160;
            MaxWidth = !double.IsNaN(state.MaxDockedSpan) ? state.MaxDockedSpan : double.PositiveInfinity;
        }
        else
        {
            MinWidth = 280;
        }
        MinHeight = ownerPanel.DockSide is DockSide.Top or DockSide.Bottom ? 80 : 160;
    }

    /// <summary>
    /// Вызывается при закрытии окна; передаёт панель-владельца для возврата содержимого в докинг.
    /// </summary>
    public event Action<DockablePanel> PanelClosing;

    /// <summary>
    /// Извлекает содержимое из окна перед закрытием для восстановления в закреплённой панели.
    /// </summary>
    /// <returns>Извлечённый элемент или <see langword="null"/>.</returns>
    public FrameworkElement ExtractContent()
    {
        if (PanelHost.Content is not FrameworkElement content)
        {
            return null;
        }

        PanelHost.Content = null;
        return content;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (ActualWidth > 0)
        {
            _ownerPanel.State.FloatingWidth = ActualWidth;
        }

        if (ActualHeight > 0)
        {
            _ownerPanel.State.FloatingHeight = ActualHeight;
        }

        PanelClosing?.Invoke(_ownerPanel);
        base.OnClosed(e);
    }
}