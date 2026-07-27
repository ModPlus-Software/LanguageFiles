namespace LangFilesEditor.Core.Application;

using Abstractions;
using Services;

/// <summary>
/// Реализация <see cref="IExtensionHost"/> — единственная точка входа расширений в core.
/// </summary>
public sealed class ExtensionHost : IExtensionHost
{
    private readonly List<LangFilesEditorToolbarCommand> _toolbarCommands = [];

    /// <summary>
    /// Хостит расширения и вызывает <see cref="ILangFilesEditorExtension.Register"/> для каждого расширения.
    /// </summary>
    /// <param name="session">Узкий read-only срез сессии для расширений.</param>
    /// <param name="commands">Реализация команд сохранения.</param>
    /// <param name="moduleEditor">Сервис мутации модулей и entries.</param>
    /// <param name="operations">Сервис фоновых операций с progress bar.</param>
    /// <param name="diagnostics">todo:</param>
    /// <param name="extensions">Набор подключаемых расширений.</param>
    public ExtensionHost(
        IExtensionEditorSession session,
        IEditorCommands commands,
        IModuleEditor moduleEditor,
        BackgroundOperationService operations,
        IDiagnosticsPublisher diagnostics,
        IEnumerable<ILangFilesEditorExtension> extensions)
    {
        Session = session;
        Commands = commands;
        Modules = moduleEditor;
        Operations = operations;
        Diagnostics = diagnostics;
        
        // todo: пу-пу-пу. А должно ли оно быть так? Наверное... Скорее всего при инициализации конркетных фрагментов core должно быть обращение сюда или ещё куда-то за списком и попытка их инициализировать.... Мне так кажется.
        foreach (var extension in extensions)
        {
            extension.Register(this);
        }
    }
    
    /// <inheritdoc />
    public IExtensionEditorSession Session { get; }
    
    // todo: тоже здесь вот под вопросом а нужно ли оно... (с учётом всех остальных замечаний[в том числе, которые ниже по списку идут])
    /// <inheritdoc />
    public IEditorCommands Commands { get; }
    
    // todo: Не то наименование и вопрос к функционалцу.
    /// <inheritdoc />
    public IModuleEditor Modules { get; }
    
    // todo: а зачем? Странно оно. Не должно быть так словно.
    /// <inheritdoc />
    public IBackgroundOperationService Operations { get; }
    
    /// <inheritdoc />
    public IDiagnosticsPublisher Diagnostics { get; }
    
    // todo: здесь using более конкретный можно написать...
    /// <inheritdoc />
    public System.Collections.ObjectModel.ObservableCollection<Models.Domain> Domains => Session.Domains;
    
    // todo: ну вот как я выше и писал о регистрации... словно это немного не то и не окей. Да, действительно было бы логично в описании контракта просто обязать пользователя какие-то элементы иметь по интерфейсу, с которыми программа могла бы работать и сама их регистрировала, а не вот так вот.
    /// <inheritdoc />
    public void RegisterToolbarCommand(LangFilesEditorToolbarCommand command) => _toolbarCommands.Add(command);
    
    /// <inheritdoc />
    public IReadOnlyList<LangFilesEditorToolbarCommand> ToolbarCommands => _toolbarCommands;
    
    // todo: возможно здесь стоит добавить какие-то ещё штуки, например добавление координаторов в store...? нз.
}