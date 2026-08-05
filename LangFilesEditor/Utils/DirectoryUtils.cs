namespace LangFilesEditor.Utils;

using Exceptions;

/// <summary>
/// Утилиты для определения путей к каталогам на диске.
/// </summary>
public class DirectoryUtils
{
    /// <summary>
    /// Возвращает корневой каталог решения — тот, в котором лежит каталог файлов локализации.
    /// </summary>
    /// <returns>Полный путь к корневому каталогу решения.</returns>
    /// <exception cref="CriticalException">Если каталог файлов локализации не найден.</exception>
    /// <remarks>
    /// Каталог вычисляется от найденного каталога локализации, а не поиском папки с заданным
    /// именем: имя папки решения на расположение данных влиять не должно. Разбор — в
    /// <see cref="LanguageFilesLocator"/>.
    /// </remarks>
    public static string GetSolutionDirectory() => LanguageFilesLocator.ResolveRoot();
}
