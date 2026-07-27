namespace LangFilesEditor.UI.Windows.MainWindow;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

/// <summary>
/// Сборка и показ контекстного меню по клику на иконку главного окна (View → Windows).
/// </summary>
internal static class WindowIconMenuBuilder
{
    /// <summary>
    /// Создаёт меню с чекбоксами видимости всех зарегистрированных закрепляемых панелей.
    /// </summary>
    /// <param name="menuVm">ViewModel меню панелей главного окна.</param>
    /// <returns>Готовое контекстное меню для показа у иконки окна.</returns>
    public static ContextMenu Create(WindowPanelsMenuVM menuVm)
    {
        var menu = new ContextMenu();
        var viewItem = new MenuItem { Header = "View" };
        var windowsItem = new MenuItem { Header = "Windows" };
        foreach (var panel in menuVm.Panels)
        {
            var checkItem = new MenuItem
            {
                Header = panel.DisplayName,
                IsCheckable = true
            };
            checkItem.SetBinding(
                MenuItem.IsCheckedProperty,
                new Binding(nameof(WindowPanelMenuItemVm.IsVisible))
                {
                    Source = panel,
                    Mode = BindingMode.TwoWay
                });
            windowsItem.Items.Add(checkItem);
        }
        
        viewItem.Items.Add(windowsItem);
        menu.Items.Add(viewItem);
        return menu;
    }
    
    /// <summary>
    /// Открывает меню в левом верхнем углу окна (область системной иконки заголовка).
    /// </summary>
    /// <param name="window">Главное окно редактора.</param>
    /// <param name="menu">Меню, созданное через <see cref="Create"/>.</param>
    public static void ShowAtWindowIcon(Window window, ContextMenu menu)
    {
        menu.PlacementTarget = window;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
        var iconPoint = window.PointToScreen(new Point(0, 0));
        menu.HorizontalOffset = iconPoint.X;
        menu.VerticalOffset = iconPoint.Y;
        menu.IsOpen = true;
    }
}