namespace LangFilesEditor.Helpers;

using Models;
using UI.Windows.MainWindow.WorkSpace;

/// <summary>
/// Вспомогательные методы расчёта позиций строк в сетке рабочей области при работе с модулями.
/// </summary>
public static class WorkspaceGridLayoutHelper
{
    /// <summary>
    /// Находит индекс строки-заголовка указанного модуля в списке строк сетки.
    /// </summary>
    /// <param name="rows">Текущий список строк рабочей области.</param>
    /// <param name="module">Модуль, заголовок которого ищется.</param>
    /// <returns>Индекс заголовка или <c>-1</c>, если заголовок не найден.</returns>
    public static int FindHeaderIndex(IList<WorkspaceGridRow> rows, Module module)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i] is ModuleHeaderGridRow header && ReferenceEquals(header.Module, module))
            {
                return i;
            }
        }
        
        return -1;
    }
    
    /// <summary>
    /// Находит индекс строки указанной записи перевода внутри модуля.
    /// </summary>
    /// <param name="rows">Текущий список строк рабочей области.</param>
    /// <param name="module">Модуль, которому принадлежит запись.</param>
    /// <param name="entry">Искомая запись перевода.</param>
    /// <returns>Индекс строки записи или <c>-1</c>, если строка не найдена.</returns>
    public static int IndexOfEntryRow(IList<WorkspaceGridRow> rows, Module module, TranslationEntry entry)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i] is TranslationEntryGridRow row
                && ReferenceEquals(row.Module, module)
                && ReferenceEquals(row.Entry, entry))
            {
                return i;
            }
        }
        
        return -1;
    }
    
    /// <summary>
    /// Определяет индекс вставки строки записи так, чтобы порядок строк совпадал с порядком
    /// <see cref="Module.Items"/>: сразу после ближайшей предыдущей записи модуля, уже имеющей строку.
    /// </summary>
    /// <param name="rows">Текущий список строк рабочей области.</param>
    /// <param name="module">Модуль записи.</param>
    /// <param name="entry">Вставляемая запись перевода.</param>
    /// <returns>Индекс вставки строки записи.</returns>
    public static int GetEntryRowInsertIndex(IList<WorkspaceGridRow> rows, Module module, TranslationEntry entry)
    {
        var headerIndex = FindHeaderIndex(rows, module);
        if (headerIndex < 0)
        {
            return rows.Count;
        }
        
        var itemIndex = module.Items.IndexOf(entry);
        if (itemIndex < 0)
        {
            return GetInsertIndexAfterModule(rows, module);
        }
        
        for (var i = itemIndex - 1; i >= 0; i--)
        {
            var precedingRowIndex = IndexOfEntryRow(rows, module, module.Items[i]);
            if (precedingRowIndex >= 0)
            {
                return precedingRowIndex + 1;
            }
        }
        
        return headerIndex + 1;
    }
    
    /// <summary>
    /// Проверяет, есть ли в сетке строки перевода, принадлежащие указанному модулю.
    /// </summary>
    /// <param name="rows">Текущий список строк рабочей области.</param>
    /// <param name="module">Проверяемый модуль.</param>
    /// <returns><see langword="true"/>, если у модуля есть хотя бы одна строка перевода в сетке.</returns>
    public static bool HasEntryRows(IList<WorkspaceGridRow> rows, Module module) =>
        rows.Any(r => r is TranslationEntryGridRow row && ReferenceEquals(row.Module, module));
    
    /// <summary>
    /// Возвращает индекс вставки сразу после последней строки перевода указанного модуля.
    /// </summary>
    /// <param name="rows">Текущий список строк рабочей области.</param>
    /// <param name="module">Модуль, после которого определяется позиция вставки.</param>
    /// <returns>Индекс для вставки новых строк или <see cref="IList{T}.Count"/>, если заголовок модуля не найден.</returns>
    public static int GetInsertIndexAfterModule(IList<WorkspaceGridRow> rows, Module module)
    {
        var index = FindHeaderIndex(rows, module);
        if (index < 0)
        {
            return rows.Count;
        }
        
        index++;
        
        while (index < rows.Count
               && rows[index] is TranslationEntryGridRow row
               && ReferenceEquals(row.Module, module))
        {
            index++;
        }
        
        return index;
    }
    
    /// <summary>
    /// Определяет индекс вставки заголовка модуля с учётом порядка модулей во вкладках.
    /// </summary>
    /// <param name="modules">Упорядоченный список открытых модулей.</param>
    /// <param name="rows">Текущий список строк рабочей области.</param>
    /// <param name="moduleIndex">Индекс модуля в списке <paramref name="modules"/>.</param>
    /// <returns>Индекс вставки заголовка в сетку.</returns>
    public static int HeaderInsertIndexForModule(IReadOnlyList<Module> modules, IList<WorkspaceGridRow> rows, int moduleIndex)
    {
        if (moduleIndex <= 0)
        {
            return 0;
        }
        
        return GetInsertIndexAfterModule(rows, modules[moduleIndex - 1]);
    }
    
    /// <summary>
    /// Удаляет все строки перевода указанного модуля, расположенные после его заголовка.
    /// </summary>
    /// <param name="rows">Изменяемый список строк рабочей области.</param>
    /// <param name="module">Модуль, строки которого удаляются.</param>
    /// <param name="headerIndex">Индекс строки-заголовка модуля.</param>
    public static void RemoveModuleEntryRows(IList<WorkspaceGridRow> rows, Module module, int headerIndex)
    {
        for (var i = rows.Count - 1; i > headerIndex; i--)
        {
            if (rows[i].Module != module)
            {
                continue;
            }
            
            if (rows[i] is TranslationEntryGridRow entryRow)
            {
                entryRow.Detach();
            }
            
            rows.RemoveAt(i);
        }
    }
}