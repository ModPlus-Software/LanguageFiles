namespace LangFilesEditor;

using System.Windows;

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
}