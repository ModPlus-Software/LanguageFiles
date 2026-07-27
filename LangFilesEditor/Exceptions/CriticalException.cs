namespace LangFilesEditor.Exceptions;

/// <summary>
/// Критическая ошибка инициализации или работы редактора, после которой приложение не может продолжать работу.
/// </summary>
/// <param name="msg">Текст сообщения об ошибке.</param>
public class CriticalException(string msg) : Exception(msg)
{
}