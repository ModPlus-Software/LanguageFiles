namespace LangFilesEditor.Utils;

using System.Globalization;
using System.IO;
using Exceptions;
using Helpers;

/// <summary>
/// Поиск каталога файлов локализации относительно запущенного приложения.
/// </summary>
/// <remarks>
/// Каталог ищется по содержимому, а не по имени каталога решения. Прежняя реализация
/// поднималась вверх до папки с именем <c>LanguageFiles</c> и считала её корнем решения,
/// поэтому переименование папки решения роняло редактор на старте, а переименование самой
/// папки с локализацией оставляло пустое дерево без единого сообщения.
/// <para>
/// Порядок обхода — от каталога приложения вверх, поэтому оба варианта раскладки работают
/// без настройки: рядом с исполняемым файлом (развёрнутая сборка) и в корне решения
/// при запуске из <c>bin\Debug\net8.0-windows</c>. Считать уровни вверх нельзя:
/// их число зависит от конфигурации и целевой платформы сборки.
/// </para>
/// </remarks>
public static class LanguageFilesLocator
{
    /// <summary>
    /// Имя каталога с языковыми подпапками.
    /// </summary>
    public const string DirectoryName = "LanguageFiles";

    private const string VersionFileName = "Version.txt";

    private static string _resolvedDirectory;

    /// <summary>
    /// Возвращает каталог файлов локализации.
    /// </summary>
    /// <returns>Полный путь к каталогу файлов локализации.</returns>
    /// <exception cref="CriticalException">Если каталог не найден ни на одном уровне.</exception>
    public static string Resolve()
    {
        if (_resolvedDirectory != null)
        {
            return _resolvedDirectory;
        }

        var probedPaths = new List<string>();
        _resolvedDirectory = Find(probedPaths)
                             ?? throw new CriticalException(
                                 EditorStrings.FormatLanguageFilesDirectoryNotFound(probedPaths));

        return _resolvedDirectory;
    }

    /// <summary>
    /// Возвращает каталог, в котором лежит каталог файлов локализации: рядом с ним
    /// размещается остальное хозяйство редактора, например каталог логов.
    /// </summary>
    /// <returns>Полный путь к родительскому каталогу.</returns>
    /// <exception cref="CriticalException">Если каталог файлов локализации не найден.</exception>
    public static string ResolveRoot() => Directory.GetParent(Resolve())!.FullName;

    /// <summary>
    /// Проверяет, похож ли каталог на каталог файлов локализации.
    /// </summary>
    /// <param name="path">Проверяемый путь.</param>
    /// <returns><c>true</c>, если каталог содержит данные локализации.</returns>
    public static bool IsLanguageFilesDirectory(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return false;
        }

        try
        {
            if (File.Exists(Path.Combine(path, VersionFileName)))
            {
                return true;
            }

            // Version.txt может отсутствовать в урезанной копии — тогда каталог опознаётся
            // по языковым подпапкам.
            return HasLanguageSubdirectory(path);
        }
        catch (UnauthorizedAccessException)
        {
            // Каталог существует, но недоступен: это не наш каталог, а не повод падать.
            // Ошибка отсюда всплыла бы не как CriticalException и обошла бы обработчик в App.
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Проверяет наличие хотя бы одной языковой подпапки с xml-файлами.
    /// </summary>
    private static bool HasLanguageSubdirectory(string path)
    {
        foreach (var candidate in Directory.EnumerateDirectories(path))
        {
            // Имя подпапки обязано быть кодом культуры. Без этой проверки каталогом
            // локализации притворяется любая папка, у которой есть вложенная папка
            // с xml-файлом, — например корень репозитория: в .git лежит свой xml.
            if (!IsCultureName(Path.GetFileName(candidate)))
            {
                continue;
            }

            if (Directory.EnumerateFiles(candidate, "*.xml", SearchOption.TopDirectoryOnly).Any())
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCultureName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        try
        {
            // predefinedOnly: без него ICU принимает за культуру произвольную строку,
            // и проверка снова перестала бы что-либо отсеивать.
            CultureInfo.GetCultureInfo(name, predefinedOnly: true);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private static string Find(ICollection<string> probedPaths)
    {
        // AppContext.BaseDirectory, а не Assembly.Location: у одиночной публикации Location
        // пустой, и вычисление каталога падало бы на пустой строке.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, DirectoryName);
            probedPaths.Add(candidate);
            if (IsLanguageFilesDirectory(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
