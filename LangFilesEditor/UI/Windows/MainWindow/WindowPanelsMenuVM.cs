namespace LangFilesEditor.UI.Windows.MainWindow;

using System.Collections.ObjectModel;
using Infrastructure.Docking;

/// <summary>
/// ViewModel меню видимости закрепляемых панелей главного окна.
/// </summary>
public sealed class WindowPanelsMenuVM
{
    /// <summary>
    /// Пункты меню, соответствующие закрепляемым панелям.
    /// </summary>
    public ObservableCollection<WindowPanelMenuItemVm> Panels { get; } = [];

    /// <summary>
    /// Регистрирует панели в меню видимости.
    /// </summary>
    /// <param name="dockManager">Менеджер докинга для показа и скрытия панелей.</param>
    /// <param name="panels">Закрепляемые панели, доступные в меню.</param>
    public void RegisterPanels(DockManager dockManager, params DockablePanel[] panels)
    {
        foreach (var item in Panels)
        {
            item.Unsubscribe();
        }

        Panels.Clear();
        foreach (var panel in panels)
        {
            Panels.Add(new WindowPanelMenuItemVm(panel, dockManager));
        }
    }
}