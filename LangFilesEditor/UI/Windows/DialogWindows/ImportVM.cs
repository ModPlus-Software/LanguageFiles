using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows.Input;
using Core.Abstractions;
using Models;
using Services;
using Utils;
using ModPlusAPI.Mvvm;

/// <summary>
/// ViewModel импорта строк перевода.
/// </summary>
public class ImportVM
{
    private readonly IEditorWorkspace _workspace;
    private readonly IEditorSession _session;
    
    /// <summary>
    /// Импорт строк ниже выбранной.
    /// </summary>
    public ICommand ImportRowsBelowCommand => new RelayCommand<string>(ImportRowsBelow);
    
    /// <summary>
    /// Автоимпорт с привязкой к тегам.
    /// </summary>
    public ICommand ImportRowsAutoCommand => new RelayCommand<(string, bool)>(ImportRowsAuto);
    
    /// <summary>
    /// Создаёт VM импорта.
    /// </summary>
    /// <param name="workspace">Рабочая область с выбором модуля и записи.</param>
    /// <param name="session">Сессия данных с языками проекта.</param>
    public ImportVM(IEditorWorkspace workspace, IEditorSession session)
    {
        _workspace = workspace;
        _session = session;
    }
    
    private void ImportRowsBelow(string rows)
    {
        var languages = _session.Languages;
        var languageCount = languages.Count;
        if (languageCount == 0)
        {
            return;
        }
        
        var resultRows = new List<List<string>>();
        var rowsSeparated = rows.Split(["\r\n", "\n"], StringSplitOptions.TrimEntries); // todo: это можно было бы вынести в настройки. А мб просто потом пометить что это следует вынести в настройки если найдётся время.
        var index = 0;
        var resultRow = new List<string>();
        foreach (var row in rowsSeparated)
        {
            if (string.IsNullOrEmpty(row))
            {
                continue;
            }
            
            if (index == languageCount)
            {
                index = 0;
            }
            
            if (index == 0)
            {
                if (resultRow.Count == languageCount)
                {
                    resultRows.Add(resultRow);
                }
                resultRow = [];
            }
            
            resultRow.Add(row);
            index++;
        }
        
        if (resultRow.Count == languageCount)
        {
            resultRows.Add(resultRow);
        }
        
        if (resultRows.Count == 0)
        {
            return;
        }
        
        var selectedModule = _workspace.SelectedModule;
        var translationEntryService = new TranslationEntryService(languages);
        index = selectedModule.Items.IndexOf(_workspace.SelectedTranslationEntry!) + 1;
        if (index == selectedModule.Items.Count)
        {
            foreach (var row in resultRows)
            {
                var previousName = selectedModule.Items.LastOrDefault()?.Name ?? string.Empty;
                selectedModule.AddTranslationEntry(
                    translationEntryService.GetNewTranslationEntry(previousName, row),
                    TranslationEntryAddSource.Import);
            }
            
            return;
        }
        
        foreach (var row in resultRows)
        {
            var previousName = selectedModule.Items[index - 1].Name;
            selectedModule.InsertTranslationEntry(
                index,
                translationEntryService.GetNewTranslationEntry(previousName, row),
                TranslationEntryAddSource.Import);
            index++;
        }
    }
    
    private void ImportRowsAuto((string rows, bool autoNumerate) parameters)
    {
        var languages = _session.Languages;
        var sortedRows = GetRows(parameters.rows, languages);
        if (sortedRows.Count == 0)
        {
            return;
        }
        
        var translationEntryService = new TranslationEntryService(languages);
        foreach (var key in sortedRows.Keys)
        {
            TagTextUtils.GetTagValueAndNumber(key, out string value, out var rowNumber);
            var selectedModule = _workspace.SelectedModule;
            var number = new SearchEngine().SearchLastRowWithTagValue(selectedModule, value, out int index);
            if (index == -1)
            {
                index = selectedModule.Items.Count - 1;
                number = 0;
            }
            
            var startName = $"{value}{rowNumber}";
            if (parameters.autoNumerate)
            {
                startName = translationEntryService.GetNewTranslationEntryName($"{value}{number}");
            }
            
            selectedModule.InsertTranslationEntry(
                index + 1,
                translationEntryService.GetNewTranslationEntry(startName, sortedRows[key]),
                TranslationEntryAddSource.Import);
        }
    }
    
    /// <summary>
    /// Разбирает многострочный текст на словарь «тег → значения по языкам».
    /// Строки с неполным набором значений (меньше числа языков) отбрасываются.
    /// </summary>
    /// <param name="rawCopyPaste">Сырой многострочный текст с тегами перевода.</param>
    /// <param name="languages">Коды языков проекта.</param>
    /// <returns>Словарь, где ключ — имя тега, значение — список переводов по языкам.</returns>
    private static Dictionary<string, List<string>> GetRows(string rawCopyPaste, IReadOnlyList<string> languages)
    {
        Dictionary<string, List<string>> result = [];
        var rows = rawCopyPaste.Split(["\r\n", "\n"], StringSplitOptions.TrimEntries);
        foreach (var row in rows)
        {
            var tag = TagTextUtils.GetRowTagName(row);
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }
            
            var content = TagTextUtils.StripRowOfTag(row);
            if (result.ContainsKey(tag))
            {
                result[tag].Add(content);
                continue;
            }
            
            result.Add(tag, [content]);
        }
        
        foreach (var key in result.Keys.ToList())
        {
            if (result[key].Count != languages.Count)
            {
                result.Remove(key);
            }
        }
        
        return result;
    }
}