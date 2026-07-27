namespace LangFilesEditor.Helpers;

using Models;

// todo: вообще штука абсолютно бесмысленная, т. к. получить отображаемые можно через ItemsView и только такое получение их и должно быть. Единственное, что стоит рассмотреть кейс, зачем и в каких случаях
/// <summary>
/// Общая логика отображения строк модуля с учётом фильтра поиска.
/// </summary>
public static class ModuleViewHelper
{
    /// <summary>
    /// Возвращает список строк перевода, видимых в представлении модуля с учётом активного фильтра.
    /// </summary>
    /// <param name="module">Модуль, строки которого нужно получить.</param>
    /// <returns>Список видимых <see cref="TranslationEntry"/>; если фильтр не применён — все строки модуля.</returns>
    public static List<TranslationEntry> GetVisibleEntries(Module module)
    {
        var view = module.ItemsView;
        if (view == null)
        {
            return module.Items.ToList();
        }
        
        var result = new List<TranslationEntry>();
        
        foreach (TranslationEntry item in view)
        {
            result.Add(item);
        }
        
        return result;
    }
}