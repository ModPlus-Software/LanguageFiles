namespace LangFilesEditor.Models;

// todo: <see cref="TranslationEntryEventArgs"/>
/// <summary>
/// Источник добавления <see cref="TranslationEntry"/> в модуль.
/// </summary>
public enum TranslationEntryAddSource
{
    /// <summary>
    /// Добавление пользователем через интерфейс редактора.
    /// </summary>
    User,
    
    /// <summary>
    /// Загрузка из XML-репозитория на диске.
    /// </summary>
    FileRepository,
    
    /// <summary>
    /// Импорт из расширения.
    /// </summary>
    ExternalLoader,
    
    /// <summary>
    /// Импорт из внешнего файла или диалога импорта.
    /// </summary>
    Import,
}