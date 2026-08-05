namespace LangFilesEditor.Models;

using Utils;
using ModPlusAPI.Mvvm;

/// <summary>
/// Наблюдаемое значение перевода для одного языка внутри <see cref="TranslationEntry"/>.
/// </summary>
public class ItemValue : ObservableObject
{
    private string _value;

    /// <summary>
    /// Текст перевода; типографские кавычки нормализуются к двойным при установке.
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            value = TextNormalizer.NormalizeQuotes(value);
            if (_value == value)
            {
                return;
            }

            _value = value;
            OnPropertyChanged();
        }
    }
}