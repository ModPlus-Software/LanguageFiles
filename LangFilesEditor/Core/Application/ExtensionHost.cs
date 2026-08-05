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
    /// <param name="diagnostics">Приёмник диагностики, в который расширения публикуют свои проверки.</param>
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

        foreach (var extension in extensions)
        {
            extension.Register(this);
        }
    }

    /// <inheritdoc />
    public IExtensionEditorSession Session { get; }

    /// <inheritdoc />
    public IEditorCommands Commands { get; }

    /// <inheritdoc />
    public IModuleEditor Modules { get; }

    /// <inheritdoc />
    public IBackgroundOperationService Operations { get; }

    /// <inheritdoc />
    public IDiagnosticsPublisher Diagnostics { get; }

    /// <inheritdoc />
    public System.Collections.ObjectModel.ObservableCollection<Models.Domain> Domains => Session.Domains;

    /// <inheritdoc />
    public void RegisterToolbarCommand(LangFilesEditorToolbarCommand command) => _toolbarCommands.Add(command);

    /// <inheritdoc />
    public IReadOnlyList<LangFilesEditorToolbarCommand> ToolbarCommands => _toolbarCommands;

}