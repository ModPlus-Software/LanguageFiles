using LangFilesEditor.UI.Models;

namespace LangFilesEditor.Helpers;

using System.IO;
using System.Windows;
using Models;


/// <summary>
/// Переключение тем приложения.
/// </summary>
public static class EditorThemeManager
{
    // todo: вот эти константы меня совсем не устраивают. Нужно как-то совсем от них избавиться
    private const string LightThemeSource = "UI/Windows/Dictionaries/AppThemeResources.xaml";
    private const string DarkThemeSource = "UI/Windows/Dictionaries/AppThemeDarkResources.xaml";
    
    // todo: Нет ли у меня ещё чего-то, куда я мог бы это впихнуть?
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
        ResourceDictionary existingTheme = null;
        foreach (var dictionary in merged)
        {
            var source = dictionary.Source?.OriginalString.Replace('\\', '/');
            if (source == null)
            {
                continue;
            }
            
            if (source.EndsWith("AppThemeResources.xaml", StringComparison.OrdinalIgnoreCase)
                || source.EndsWith("AppThemeDarkResources.xaml", StringComparison.OrdinalIgnoreCase))
            {
                existingTheme = dictionary;
                break;
            }
        }
        
        if (existingTheme?.Source?.OriginalString.Replace('\\', '/').EndsWith(
                Path.GetFileName(targetSource),
                StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }
        
        if (existingTheme != null)
        {
            merged.Remove(existingTheme);
        }
        
        merged.Add(new ResourceDictionary
        {
            Source = new Uri(targetSource, UriKind.Relative),
        });
    }
}