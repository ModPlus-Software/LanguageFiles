using LangFilesEditor.Services;

namespace LangFilesEditor.UI.Windows.DialogWindows;

using System.Windows;
using Core.Abstractions;

/// <summary>
/// Окно пометки строки на удаление в будущей версии.
/// </summary>
public partial class MarkForDeletionWindow
{
    private readonly IEditorWorkspace _host;
    
    /// <summary>
    /// Создаёт окно с host API.
    /// </summary>
    /// <param name="host">Рабочая область с выбранной записью перевода.</param>
    public MarkForDeletionWindow(IEditorWorkspace host)
    {
        _host = host;
        InitializeComponent();
        TbVersion.Text = ResolveExistingVersion();
    }
    
    private void Cancel_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;
    
    private void Mark_OnClick(object sender, RoutedEventArgs e)
    {
        if (!Version.TryParse(TbVersion.Text, out var version))
        {
            MessageBox.Show(
                "Enter valid version!",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }
        
        var entry = _host.SelectedTranslationEntry!;
        entry.RemovesOnVersion = version.ToString();
        entry.Comment = $"{Constants.RemoveAfterCommentPrefix}{version}";
        DialogResult = true;
    }
    
    /// <summary>
    /// Возвращает версию, для которой строка уже помечена к удалению, или пустую строку.
    /// Метка хранится в комментарии вида <c>todo remove after X.Y.Z</c>, поэтому версия
    /// извлекается из него; рантайм-поле <see cref="Models.TranslationEntry.RemovesOnVersion"/>
    /// используется как запасной источник.
    /// </summary>
    private string ResolveExistingVersion()
    {
        var entry = _host.SelectedTranslationEntry;
        if (entry == null)
        {
            return string.Empty;
        }
        
        var comment = entry.Comment;
        if (!string.IsNullOrEmpty(comment)
            && comment.StartsWith(Constants.RemoveAfterCommentPrefix, StringComparison.Ordinal))
        {
            return comment[Constants.RemoveAfterCommentPrefix.Length..].Trim();
        }
        
        return entry.RemovesOnVersion ?? string.Empty;
    }
}