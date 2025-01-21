namespace LangFilesEditor.Structure;

internal class ItemValue : ObservableObject
{
    private string _value;
    
    /// <summary>
    /// Value
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value)
                return;
            _value = value;
            OnPropertyChanged();
        }
    }
}