namespace LangFilesEditor;

using System.Windows;
using System.Windows.Controls;

internal static class TabControlExtensions
{
    private static readonly Dictionary<object, ContentPresenter> _cache = new();

    public static readonly DependencyProperty EnableContentCachingProperty =
        DependencyProperty.RegisterAttached(
            "EnableContentCaching",
            typeof(bool),
            typeof(TabControlExtensions),
            new PropertyMetadata(false, OnEnableContentCachingChanged));

    public static void SetEnableContentCaching(TabControl element, bool value) =>
        element.SetValue(EnableContentCachingProperty, value);

    public static bool GetEnableContentCaching(TabControl element) =>
        (bool)element.GetValue(EnableContentCachingProperty);

    private static void OnEnableContentCachingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TabControl tabControl)
            return;

        if ((bool)e.NewValue)
        {
            tabControl.SelectionChanged += TabControl_SelectionChanged;
        }
        else
        {
            tabControl.SelectionChanged -= TabControl_SelectionChanged;
        }
    }

    private static void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl)
            return;

        foreach (var added in e.AddedItems)
        {
            if (!_cache.TryGetValue(added, out var presenter))
            {
                var container = tabControl.ItemContainerGenerator.ContainerFromItem(added) as TabItem;
                if (container?.Content is not FrameworkElement content)
                    continue;

                var cp = new ContentPresenter { Content = content.DataContext };
                cp.ContentTemplate = container.ContentTemplate;
                _cache[added] = cp;
                container.Content = cp;
            }
            else
            {
                var container = tabControl.ItemContainerGenerator.ContainerFromItem(added) as TabItem;
                if (container != null)
                    container.Content = presenter;
            }
        }
    }
}
