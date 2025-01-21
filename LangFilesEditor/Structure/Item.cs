namespace LangFilesEditor.Structure;

internal class Item : ObservableObject
{
    private string _name;

    /// <summary>
    /// Name
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (value == _name)
                return;
            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Values
    /// </summary>
    public Dictionary<string, ItemValue> Values { get; } = [];
}