namespace LangFilesEditor;

using System.Windows;
using System.Windows.Media;

internal static class Utils
{
    public static void SafeExecute(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }
    }

    public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is T t)
            {
                return t;
            }
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }
}