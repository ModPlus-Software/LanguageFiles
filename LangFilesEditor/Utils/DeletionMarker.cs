namespace LangFilesEditor.Utils;

/// <summary>
/// Разбор и сборка комментария-пометки «удалить после версии».
/// </summary>
/// <remarks>
/// Единственное место, где знают формат метки. Раньше разбор был продублирован
/// в модели записи и в окне пометки, и оба варианта требовали точного совпадения
/// с префиксом. В файлах локализации метка записана двумя способами —
/// <c>&lt;!--todo remove after 1.2.3--&gt;</c> (пишет редактор) и
/// <c>&lt;!-- todo remove after 1.2.3 --&gt;</c> (написано вручную). XComment отдаёт
/// значение вместе с пробелами, поэтому второй вариант не распознавался.
/// <para>
/// Разбор намеренно строгий: в файлах встречаются границы диапазонов
/// <c>&lt;!-- todo remove after 1.2.3 start --&gt;</c> и <c>… end</c>. Их нельзя считать
/// пометкой одной записи — иначе снятие пометки удалило бы границу диапазона.
/// </para>
/// </remarks>
public static class DeletionMarker
{
    /// <summary>
    /// Пытается извлечь версию из комментария-пометки.
    /// </summary>
    /// <param name="comment">Текст комментария записи перевода.</param>
    /// <param name="version">Версия без окружающих пробелов, если пометка распознана.</param>
    /// <returns><c>true</c>, если комментарий является пометкой к удалению.</returns>
    public static bool TryGetVersion(string comment, out string version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(comment))
        {
            return false;
        }

        // Префикс хранится с завершающим пробелом (он нужен при записи), а при разборе
        // пробел проверяется отдельно — иначе «todo remove afterX» сошло бы за пометку.
        var prefix = Constants.RemoveAfterCommentPrefix.TrimEnd();
        var trimmed = comment.Trim();

        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = trimmed[prefix.Length..];
        if (rest.Length == 0 || !char.IsWhiteSpace(rest[0]))
        {
            return false;
        }

        // Остаток обязан быть номером версии и ничем больше: «1.2.3 start» и «1.2.3 end» —
        // границы диапазона, а не пометка конкретной записи.
        rest = rest.Trim();
        if (rest.Length == 0 || !System.Version.TryParse(rest, out _))
        {
            return false;
        }

        version = rest;
        return true;
    }

    /// <summary>
    /// Проверяет, является ли комментарий пометкой к удалению.
    /// </summary>
    /// <param name="comment">Текст комментария записи перевода.</param>
    /// <returns><c>true</c>, если комментарий является пометкой к удалению.</returns>
    public static bool IsMarked(string comment) => TryGetVersion(comment, out _);

    /// <summary>
    /// Собирает текст комментария-пометки для указанной версии.
    /// </summary>
    /// <param name="version">Версия, после которой ключ удаляется.</param>
    /// <returns>Текст комментария.</returns>
    public static string Build(string version) => $"{Constants.RemoveAfterCommentPrefix}{version}";
}
