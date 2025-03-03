namespace LangFilesEditor;

using System.Reflection;
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

    /// <summary>
    /// Выполнить асинхронный метод в обертке try{} catch{} без возврата bool значения
    /// </summary>
    /// <param name="func">Выполняемый метод</param>
    public static async void SafeExecuteAsync(Func<Task> func)
    {
        try
        {
            await func.Invoke();
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

    // https://stackoverflow.com/a/69081
    public static void CopyToClipboard(string str)
    {
        for (var i = 0; i < 10; i++)
        {
            try
            {
                Clipboard.SetText(str);
                return;
            }
            catch
            {
                // ignore
            }

            Thread.Sleep(10);
        }
    }

    public static string GetFromClipboard()
    {
        for (var i = 0; i < 10; i++)
        {
            try
            {
                return Clipboard.GetText();
            }
            catch
            {
                // ignore
            }

            Thread.Sleep(10);
        }

        return string.Empty;
    }
}