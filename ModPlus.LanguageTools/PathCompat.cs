namespace ModPlus.LanguageTools;

/// <summary>
/// Работа с путями, одинаковая для net8.0 и netstandard2.0.
/// </summary>
internal static class PathCompat
{
    /// <summary>
    /// Относительный путь от папки до файла (для сообщений лога).
    /// </summary>
    /// <param name="relativeTo">Папка</param>
    /// <param name="path">Файл</param>
    public static string GetRelativePath(string relativeTo, string path)
    {
#if NETSTANDARD2_0
        var root = Path.GetFullPath(relativeTo);
        if (!root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            root += Path.DirectorySeparatorChar;

        var full = Path.GetFullPath(path);
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : full;
#else
        return Path.GetRelativePath(relativeTo, path);
#endif
    }
}
