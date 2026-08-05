using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Models;
using Utils;

/// <summary>
/// Панель навигации по дереву domain и module; синхронизирует выбор с <see cref="NavBarVM"/>.
/// </summary>
public partial class NavBar
{
    private ScrollViewer _navScrollViewer;

    /// <summary>
    /// Инициализирует разметку панели навигации.
    /// </summary>
    public NavBar()
    {
        InitializeComponent();
    }

    private void NavTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is not NavBarVM vm)
        {
            return;
        }

        switch (e.NewValue)
        {
            case Module module:
                vm.SelectedModule = module;
                break;
            case Domain domain:
                vm.SelectedDomain = domain;
                break;
        }
    }

    private void NavTreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender))
        {
            return;
        }

        if (sender is not TreeViewItem { DataContext: Domain domain })
        {
            return;
        }

        domain.IsExpanded = true;
        PreserveScrollOffset();
    }

    private void NavTreeViewItem_Collapsed(object sender, RoutedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender))
        {
            return;
        }

        if (sender is not TreeViewItem { DataContext: Domain domain })
        {
            return;
        }

        domain.IsExpanded = false;

        PreserveScrollOffset();
    }

    private void PreserveScrollOffset()
    {
        _navScrollViewer ??= WpfUtils.FindVisualChild<ScrollViewer>(NavTree);
        if (_navScrollViewer == null)
        {
            return;
        }

        var offset = _navScrollViewer.VerticalOffset;
        Dispatcher.BeginInvoke(() => _navScrollViewer.ScrollToVerticalOffset(offset), DispatcherPriority.Loaded);
    }
}