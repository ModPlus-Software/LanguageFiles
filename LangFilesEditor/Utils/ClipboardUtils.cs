namespace LangFilesEditor.Utils;

using System.Windows;

/// <summary>
/// Утилиты для работы с буфером обмена Windows (чтение и запись текста с повторными попытками).
/// </summary>
public class ClipboardUtils
{
    // https://stackoverflow.com/a/69081
    /// <summary>
    /// Копирует текст в буфер обмена с предварительной очисткой и повторными попытками при временных сбоях.
    /// </summary>
    /// <remarks>Перед копированием буфер обмена всегда очищается.</remarks>
    /// <param name="str">Текст для помещения в буфер обмена.</param>
    public static void CopyToClipboard(string str)
    {
        Clipboard.Clear();
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
    
    /// <summary>
    /// Читает текст из буфера обмена с повторными попытками при временных сбоях.
    /// </summary>
    /// <returns>Текст из буфера обмена или пустая строка, если чтение не удалось.</returns>
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