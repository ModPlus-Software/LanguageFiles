namespace LangFilesEditor.UI.Windows.MainWindow;

using System.ComponentModel;
using Infrastructure.Docking;
using ModPlusAPI.Mvvm;

/// <summary>
/// Пункт меню видимости одной закрепляемой панели.
/// </summary>
public sealed class WindowPanelMenuItemVm : ObservableObject
{
    private readonly DockablePanel _panel;
    private readonly DockManager _dockManager;
    
    /// <summary>
    /// Создаёт пункт меню для управления видимостью панели.
    /// </summary>
    /// <param name="panel">Закрепляемая панель.</param>
    /// <param name="dockManager">Менеджер докинга для показа и скрытия панели.</param>
    public WindowPanelMenuItemVm(DockablePanel panel, DockManager dockManager)
    {
        _panel = panel;
        _dockManager = dockManager;
        State = panel.State;
        State.PropertyChanged += OnStatePropertyChanged;
    }
    
    /// <summary>
    /// Состояние панели (видимость, заголовок, сторона докинга).
    /// </summary>
    public DockPanelState State { get; }
    
    /// <summary>
    /// Отображаемое имя панели в меню.
    /// </summary>
    public string DisplayName => State.Title;
    
    /// <summary>
    /// Видима ли панель; изменение вызывает показ или скрытие через менеджер докинга.
    /// </summary>
    public bool IsVisible
    {
        get => State.IsVisible;
        set
        {
            if (State.IsVisible == value)
            {
                return;
            }
            if (value)
            {
                _dockManager.ShowPanel(_panel);
            }
            else
            {
                _dockManager.HidePanel(_panel);
            }
        }
    }
    
    private void OnStatePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DockPanelState.IsVisible))
        {
            OnPropertyChanged(nameof(IsVisible));
        }
    }
}