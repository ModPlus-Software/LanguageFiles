namespace LangFilesEditor.UI.Windows.MainWindow;

using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Infrastructure.Docking;

/// <summary>
/// Главное окно редактора: докинг панелей, иконка в заголовке с меню View/Windows и инициализация VM.
/// </summary>
public partial class MainWindow
{
    private const int WmNcLButtonDown = 0x00A1;
    private const int HtSysMenu = 3;
    private WindowPanelsMenuVM _windowPanelsMenu;
    private bool _closeWithoutSave;

    /// <summary>
    /// Создаёт окно, связывает <see cref="MainWindowVM"/> и подписывается на закрытие по запросу VM.
    /// </summary>
    public MainWindow(MainWindowVM dataContext)
    {
        InitializeComponent();
        DataContext = dataContext;
        Loaded += OnLoaded;
        Closing += OnClosing;
        dataContext.RequestCloseEvent += () =>
        {
            _closeWithoutSave = true;
            Close();
        };
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            source.AddHook(WindowProc);
        }
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmNcLButtonDown && wParam == HtSysMenu)
        {
            ShowWindowIconMenu();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void ShowWindowIconMenu()
    {
        if (_windowPanelsMenu == null)
        {
            return;
        }

        var menu = WindowIconMenuBuilder.Create(_windowPanelsMenu);
        WindowIconMenuBuilder.ShowAtWindowIcon(this, menu);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        RegisterDockPanels();
        if (DataContext is MainWindowVM vm)
        {
            _ = vm.InitializeNavigationAsync();
        }
    }

    private void RegisterDockPanels()
    {
        var vm = (MainWindowVM)DataContext;
        NavDockPanel.State = vm.NavPanel;
        ToolDockPanel.State = vm.ToolPanel;
        SearchDockPanel.State = vm.SearchPanel;
        AttributesDockPanel.State = vm.AttributesPanel;
        SetPanelContentDataContext(NavDockPanel, vm.NavBarVM);
        SetPanelContentDataContext(ToolDockPanel, vm.ToolBarVM);
        SetPanelContentDataContext(SearchDockPanel, vm.SearchBarVM);
        SetPanelContentDataContext(AttributesDockPanel, vm.Session);
        DockSite.RegisterPanel(NavDockPanel);
        DockSite.RegisterPanel(ToolDockPanel);
        DockSite.RegisterPanel(SearchDockPanel);
        DockSite.RegisterPanel(AttributesDockPanel);
        vm.WindowPanelsMenu.RegisterPanels(
            DockSite.Manager,
            SearchDockPanel,
            ToolDockPanel,
            AttributesDockPanel,
            NavDockPanel);
        _windowPanelsMenu = vm.WindowPanelsMenu;
    }

    private static void SetPanelContentDataContext(DockablePanel panel, object dataContext)
    {
        if (panel.PanelContent is FrameworkElement element)
        {
            element.DataContext = dataContext;
        }
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        if (_closeWithoutSave)
        {
            return;
        }

        if (DataContext is MainWindowVM vm && !vm.TrySaveOnExit())
        {
            e.Cancel = true;
        }
    }
}