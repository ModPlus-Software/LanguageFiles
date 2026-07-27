namespace LangFilesEditor.Core.Abstractions;

/// <summary>
/// Расширение редактора локализации (необходимо наследоваться от этого интерфейса).
/// Реализации живут в папке Extensions и подключаются через <see cref="IExtensionHost"/>.
/// </summary>
public interface ILangFilesEditorExtension
{
    /// <summary>
    /// Отображаемое имя расширения.
    /// </summary>
    string DisplayName { get; }
    
    // todo: не совсем понятное наименование
    /// <summary>
    /// Регистрирует расширение.
    /// </summary>
    /// <param name="host">Host, предоставляющий доступ к сессии, командам и сервисам редактора.</param>
    void Register(IExtensionHost host);
}