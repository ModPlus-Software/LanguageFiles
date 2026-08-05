using LangFilesEditor.UI.Models;

namespace LangFilesEditor.Helpers;

using System.IO;
using System.Windows;
using Models;

/// <summary>
/// Переключение тем приложения.
/// </summary>
/// <remarks>
/// Словарь темы заменяется целиком. Это ровно один обход визуального дерева: WPF
/// переспрашивает у каждого элемента неявный стиль и все ссылки DynamicResource.
/// Стоимость операции пропорциональна размеру дерева, поэтому оно должно оставаться
/// небольшим — см. виртуализацию грида в TranslationEntriesGridResources.xaml.
/// <para>
/// Перекрасить кисти на месте (это был бы обход нулевого размера) нельзя: словарь,
/// подключённый к <see cref="Application.Resources"/>, получает владельца-приложение,
/// а ResourceDictionary при этом запечатывает свои значения — все Freezable в нём
/// оказываются замороженными, и присвоение Color бросает исключение.
/// </para>
/// </remarks>
public static class EditorThemeManager
{
    private const string LightThemeSource = "UI/Windows/Dictionaries/AppThemeResources.xaml";
    private const string DarkThemeSource = "UI/Windows/Dictionaries/AppThemeDarkResources.xaml";

    /// <summary>
    /// Применяет выбранную тему ко всему приложению.
    /// </summary>
    /// <param name="theme">Тема интерфейса.</param>
    public static void Apply(EditorAppTheme theme)
    {
        var app = Application.Current;
        if (app == null)
        {
            return;
        }

        var targetSource = theme == EditorAppTheme.Dark ? DarkThemeSource : LightThemeSource;
        var merged = app.Resources.MergedDictionaries;
        var existingIndex = -1;
        for (var i = 0; i < merged.Count; i++)
        {
            var source = merged[i].Source?.OriginalString.Replace('\\', '/');
            if (source == null)
            {
                continue;
            }

            if (source.EndsWith("AppThemeResources.xaml", StringComparison.OrdinalIgnoreCase)
                || source.EndsWith("AppThemeDarkResources.xaml", StringComparison.OrdinalIgnoreCase))
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0
            && merged[existingIndex].Source?.OriginalString.Replace('\\', '/').EndsWith(
                Path.GetFileName(targetSource),
                StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        var themeDictionary = new ResourceDictionary
        {
            Source = new Uri(targetSource, UriKind.Relative),
        };

        // Замена ровно на прежнем месте, а не удаление с добавлением в конец:
        // порядок словарей задаёт приоритет ключей, и после переключения темы
        // он должен остаться таким же, как объявлено в App.xaml.
        // Одно присваивание — один обход дерева; поэлементная правка словаря
        // запускала бы обход на каждый ключ.
        if (existingIndex >= 0)
        {
            merged[existingIndex] = themeDictionary;
        }
        else
        {
            merged.Insert(0, themeDictionary);
        }
    }
}
