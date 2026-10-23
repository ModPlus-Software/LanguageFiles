namespace LangFilesEditor.Models;

using ModPlusAPI.Mvvm;

/// <summary>
/// Наблюдаемое значение перевода для одного языка внутри <see cref="TranslationEntry"/>.
/// </summary>
public class ItemValue : ObservableObject
{
    private string _value;

    /// <summary>
    /// Текст перевода. Кавычки при вводе не меняются: типографские кавычки приводятся
    /// к прямым при сохранении файла и при сборке (<see cref="ModPlus.LanguageTools.QuoteNormalizer"/>).
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            OnPropertyChanged();
        }
    }
}