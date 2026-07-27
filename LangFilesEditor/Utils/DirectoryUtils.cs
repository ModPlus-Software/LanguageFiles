namespace LangFilesEditor.Utils;

using System.IO;
using System.Reflection;
using Exceptions;

/// <summary>
/// Утилиты для определения путей к каталогам на диске.
/// </summary>
public class DirectoryUtils
{
    /// <summary>
    /// Возвращает корневой каталог решения.
    /// </summary>
    /// <returns>Полный путь к корневому каталогу решения.</returns>
    /// <exception cref="CriticalException">Если корневой каталог решения не найден.</exception>
    public static string GetSolutionDirectory()
    {
        var executingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var parent = Directory.GetParent(executingDirectory!);
        while (parent != null && parent.Name != "LanguageFiles")
        {
            parent = Directory.GetParent(parent.FullName);
        }
        
        if (parent == null)
        {
            // todo: локализация
            throw new CriticalException("Не найден корневой каталог решения (LanguageFiles).");
        }
        
        return parent.FullName;
    }
}