namespace LangFilesEditor;

using System.Text.RegularExpressions;
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

    public static string StripXmlRowOfTag(string row)
    {
        var result = Regex.Replace(row.Trim(), "^<[^>]+>", string.Empty);
        result = Regex.Replace(result, "<[^>]+>$", string.Empty);
        return result;
    }

    public static string GetXmlRowTagContents(string row)
    {
        var result = string.Empty;
        var match = Regex.Match(row.Trim(), "^<[^>]+>");
        if (match.Success)
        {
            result = match.Value;
            result = result.Substring(1, result.Length - 2);
        }

        return result;
    }

    public static void GetTagValueAndNumber(string tag, out string value, out int number)
    {
        value = string.Empty;
        number = 1;

        var match = Regex.Match(tag, "\\d+$");
        if (match.Success)
        {
            int.TryParse(match.Value, out number);
            value = tag.Replace(match.Value, string.Empty);
        }
        else
        {
            value = tag;
        }
    }

    public static Color DrawingColorToMediaColor(System.Drawing.Color color)
    {
        try
        {
            return Color.FromRgb(color.R, color.G, color.B);
        }
        catch
        {
            return Colors.Chartreuse;
        }
    }
}