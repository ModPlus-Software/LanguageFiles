namespace LangFilesEditor.Core.Abstractions;

using System.Collections.ObjectModel;
using System.ComponentModel;
using Models;

/// <summary>
/// Активная сессия работы приложения, доступная в рамках расширений.
/// </summary>
/// todo: с этой штукой нужно что-то сделать.
public interface IExtensionEditorSession : INotifyPropertyChanged
{
    /// <summary>
    /// Коды языков локализации.
    /// </summary>
    IReadOnlyList<string> Languages { get; }

    /// <summary>
    /// Группы локализации.
    /// </summary>
    ObservableCollection<Domain> Domains { get; }

    /// <summary>
    /// Выбранная группа локализации.
    /// </summary>
    Domain SelectedDomain { get; }

    /// <summary>
    /// Выбранный модуль.
    /// </summary>
    Module SelectedModule { get; }

    /// <summary>
    /// Выбранная единица локализации.
    /// </summary>
    TranslationEntry SelectedTranslationEntry { get; }

    /// <summary>
    /// todo: Нужно ли это поле?
    /// В процессе ли длительная операция.
    /// </summary>
    bool IsOperationInProgress { get; }
}