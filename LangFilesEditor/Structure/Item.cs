namespace LangFilesEditor.Structure;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

internal class Item : ObservableObject
{
    private string _name;
    private bool _isVisible = true;
    private bool _hasIncorrectData;
    private bool _hasDuplicateName;
    private bool _hasDuplicateValue;
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
    /// Комментарий к этому элементу
    /// </summary>
    public string Comment { get; set; }

    /// <summary>
    /// Row background color
    /// </summary>
    public SolidColorBrush BackgroundColor => !string.IsNullOrEmpty(Comment) ? Brushes.LightSkyBlue : Brushes.White;

    /// <summary>
    /// Is visible in UI
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
                return;
            _isVisible = value;
            OnPropertyChanged();
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
            OnPropertyChanged(nameof(IsVisibleError));
        }
    }

    /// <summary>
    /// Has item with same name in parent <see cref="Node"/>
    /// </summary>
    public bool HasDuplicateName
    {
        get => _hasDuplicateName;
        set
        {
            if (_hasDuplicateName == value)
                return;
            _hasDuplicateName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsVisibleError));
        }
    }

    /// <summary>
    /// Has item with same value in parent <see cref="Node"/>
    /// </summary>
    public bool HasDuplicateValue
    {
        get => _hasDuplicateValue;
        set
        {
            if (_hasDuplicateValue == value)
                return;
            _hasDuplicateValue = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsVisibleWarning));
        }
    }

    /// <summary>
    /// Is visible error icon
    /// </summary>
    public bool IsVisibleError => HasDuplicateName || HasIncorrectData;

    /// <summary>
    /// Is visible warning icon
    /// </summary>
    public bool IsVisibleWarning => HasDuplicateValue;

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
        InvokeValidateInParent();
    }

    public void Validate()
    {
        HasIncorrectData = string.IsNullOrEmpty(Name) ||
                           char.IsDigit(Name[0]) ||
                           _values.Any(v => string.IsNullOrEmpty(v.Value.Value));
        //InvokeValidateInParent();
    }

    private void InvokeValidateInParent()
    {
        ValidateInParent?.Invoke(this, EventArgs.Empty);
    }
}