using LangFilesEditor.Models;

namespace LangFilesEditor.UI.Windows.MainWindow.WorkSpace;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Converters;
using Models;
using Utils;

/// <summary>
/// Тип строки в гриде рабочей области.
/// </summary>
public enum WorkspaceRowKind
{
    /// <summary>
    /// Заголовок модуля.
    /// </summary>
    ModuleHeader,
    
    /// <summary>
    /// Строка записи перевода.
    /// </summary>
    Entry,
}

/// <summary>
/// Базовая модель строки грида рабочей области.
/// </summary>
public abstract class WorkspaceGridRow
{
    /// <summary>
    /// Тип строки (заголовок или запись).
    /// </summary>
    public abstract WorkspaceRowKind Kind { get; }
    
    /// <summary>
    /// Модуль, к которому относится строка.
    /// </summary>
    public Module Module { get; protected init; }
}

/// <summary>
/// Строка-заголовок модуля в гриде рабочей области.
/// </summary>
public sealed class ModuleHeaderGridRow : WorkspaceGridRow
{
    /// <summary>
    /// Создаёт строку заголовка модуля.
    /// </summary>
    /// <param name="module">Модуль, для которого формируется заголовок.</param>
    /// <param name="showDomainInTitle">Показывать ли имя домена в заголовке.</param>
    public ModuleHeaderGridRow(Module module, bool showDomainInTitle = false)
    {
        Module = module;
        ShowDomainInTitle = showDomainInTitle;
    }
    
    /// <inheritdoc />
    public override WorkspaceRowKind Kind => WorkspaceRowKind.ModuleHeader;
    
    /// <summary>
    /// Показывать ли имя домена в заголовке.
    /// </summary>
    public bool ShowDomainInTitle { get; }
    
    /// <summary>
    /// Имя домена модуля.
    /// </summary>
    public string DomainName => Module.Group?.Name ?? string.Empty;
    
    /// <summary>
    /// Имя модуля.
    /// </summary>
    public string ModuleName => Module.Name;
    
    /// <summary>
    /// Заголовок для отображения (с доменом или без).
    /// </summary>
    public string Title => ShowDomainInTitle && !string.IsNullOrEmpty(DomainName)
        ? $"{DomainName} — {ModuleName}" : ModuleName;
    
    /// <summary>
    /// Заголовок с переносами для отображения в ячейке грида.
    /// </summary>
    public string WrappedTitle => ModuleTitleFormatter.WrapForDisplay(Title);
}

/// <summary>
/// Строка записи перевода в гриде рабочей области.
/// </summary>
public sealed class TranslationEntryGridRow : WorkspaceGridRow, INotifyPropertyChanged
{
    /// <summary>
    /// Создаёт строку записи перевода.
    /// </summary>
    /// <param name="module">Модуль, содержащий запись.</param>
    /// <param name="entry">Запись перевода.</param>
    public TranslationEntryGridRow(Module module, TranslationEntry entry)
    {
        Module = module;
        Entry = entry;
        Entry.PropertyChanged += OnEntryPropertyChanged;
    }
    
    /// <inheritdoc />
    public override WorkspaceRowKind Kind => WorkspaceRowKind.Entry;
    
    /// <summary>
    /// Запись перевода, представленная строкой.
    /// </summary>
    public TranslationEntry Entry { get; }
    
    /// <summary>
    /// Фон строки грида; вычисляется из <see cref="TranslationEntry.RowVisualState"/>
    /// через <see cref="RowVisualStateToBrushConverter"/> (единая точка соответствия состояний и кистей).
    /// </summary>
    public SolidColorBrush RowBackground => RowVisualStateToBrushConverter.ToBrush(Entry.RowVisualState);
    
    /// <summary>
    /// Подсказка строки грида; прокси для <see cref="TranslationEntry.RowToolTip"/>.
    /// </summary>
    public string RowToolTip => Entry.RowToolTip;
    
    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;
    
    /// <summary>
    /// Отписывается от изменений записи перед удалением строки из грида.
    /// </summary>
    public void Detach() => Entry.PropertyChanged -= OnEntryPropertyChanged;
    
    /// <summary>
    /// Принудительно обновляет привязки фона и подсказки строки.
    /// </summary>
    public void RefreshRowPresentation()
    {
        OnPropertyChanged(nameof(RowBackground));
        OnPropertyChanged(nameof(RowToolTip));
    }
    
    private void OnEntryPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (AffectsRowBackground(e.PropertyName))
        {
            OnPropertyChanged(nameof(RowBackground));
        }
        
        if (e.PropertyName == nameof(TranslationEntry.RowToolTip))
        {
            OnPropertyChanged(nameof(RowToolTip));
        }
    }
    
    private static bool AffectsRowBackground(string propertyName) => propertyName switch
    {
        nameof(TranslationEntry.RowVisualState) => true,
        nameof(TranslationEntry.Comment) => true,
        nameof(EntryDiagnosticState.HasIncorrectData) => true,
        nameof(EntryDiagnosticState.HasDuplicateName) => true,
        nameof(EntryDiagnosticState.HasDuplicateValue) => true,
        nameof(EntryDiagnosticState.ExtensionDiagnostic) => true,
        _ => false,
    };
    
    private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}