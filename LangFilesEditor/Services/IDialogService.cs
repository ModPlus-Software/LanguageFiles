namespace LangFilesEditor.Services;

/// <summary>
/// Контракт сервиса модальных диалогов редактора локализации.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Открывает диалог импорта с флажком дополнительной опции.
    /// </summary>
    /// <returns>Результат диалога или <c>null</c>, если окно закрыто без выбора.</returns>
    public bool? ShowImportWindowWithCheckbox();

    /// <summary>
    /// Открывает стандартный диалог импорта.
    /// </summary>
    /// <returns>Результат диалога или <c>null</c>, если окно закрыто без выбора.</returns>
    public bool? ShowImportWindow();

    /// <summary>
    /// Открывает диалог слияния языковых файлов.
    /// </summary>
    /// <returns>Результат диалога или <c>null</c>, если окно закрыто без выбора.</returns>
    public bool? ShowMergerWindow();

    /// <summary>
    /// Открывает диалог пометки ключей к удалению.
    /// </summary>
    /// <returns>Результат диалога или <c>null</c>, если окно закрыто без выбора.</returns>
    public bool? ShowMarkForDeletionWindow();

    /// <summary>
    /// Открывает окно настроек редактора.
    /// </summary>
    public void ShowSettingsWindow();

    /// <summary>
    /// Показывает информационное сообщение пользователю.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    public void ShowMessageWindow(string message);

    /// <summary>
    /// Показывает диалог с вопросом «Да/Нет».
    /// </summary>
    /// <param name="message">Текст вопроса.</param>
    /// <returns><c>true</c>, если пользователь подтвердил действие.</returns>
    public bool ShowQuestionWindow(string message);
}