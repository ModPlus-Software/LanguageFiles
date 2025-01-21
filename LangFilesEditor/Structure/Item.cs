namespace LangFilesEditor.Structure;

using System.Collections.ObjectModel;
using System.ComponentModel;

internal class Item : ObservableObject
{
    private string _name;
    private bool _hasIncorrectData;
    private bool _hasDuplicate;
    private readonly Dictionary<string, ItemValue> _values;

    public Item()
    {
        _values = [];
        Values = new ReadOnlyDictionary<string, ItemValue>(_values);
        Validate();
    }

    /// <summary>
    /// Need validate in parent event
    /// </summary>
    public event EventHandler ValidateInParent;

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
            Validate();
        }
    }

    /// <summary>
    /// Has incorrect data
    /// </summary>
    public bool HasIncorrectData
    {
        get => _hasIncorrectData;
        set
        {
            if (_hasIncorrectData == value)
                return;
            _hasIncorrectData = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsVisibleWarning));
        }
    }

    /// <summary>
    /// Has duplicate in parent <see cref="Node"/>
    /// </summary>
    public bool HasDuplicate
    {
        get => _hasDuplicate;
        set
        {
            if (_hasDuplicate == value)
                return;
            _hasDuplicate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsVisibleWarning));
        }
    }

    /// <summary>
    /// Is visible warning icon
    /// </summary>
    public bool IsVisibleWarning => HasDuplicate || HasIncorrectData;

    /// <summary>
    /// Values
    /// </summary>
    public IReadOnlyDictionary<string, ItemValue> Values { get; }

    /// <summary>
    /// Add new <see cref="ItemValue"/>
    /// </summary>
    /// <param name="languageName">Language name</param>
    /// <param name="itemValue">New <see cref="ItemValue"/></param>
    public void Add(string languageName, ItemValue itemValue)
    {
        itemValue.PropertyChanged += ItemValueOnPropertyChanged;
        _values[languageName] = itemValue;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }

    private void ItemValueOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Validate();
    }

    private void Validate()
    {
        HasIncorrectData = string.IsNullOrEmpty(Name) ||
                           char.IsDigit(Name[0]) ||
                           _values.Any(v => string.IsNullOrEmpty(v.Value.Value));
        InvokeValidateInParent();
    }

    private void InvokeValidateInParent()
    {
        ValidateInParent?.Invoke(this, EventArgs.Empty);
    }
}