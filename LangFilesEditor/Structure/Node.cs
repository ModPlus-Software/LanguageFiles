namespace LangFilesEditor.Structure;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

internal class Node : ObservableObject
{
    private bool _hasIncorrectData;
    private string _searchString;
    private bool _hasItemsWithSameValue;

    public Node(string name)
    {
        Name = name;
        Items = [];
        Items.CollectionChanged += ItemsOnCollectionChanged;
    }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Items
    /// </summary>
    public ObservableCollection<Item> Items { get; }

    /// <summary>
    /// Search string
    /// </summary>
    public string SearchString
    {
        get => _searchString;
        set
        {
            if (_searchString == value)
                return;
            _searchString = value;
            OnPropertyChanged();
            Search();
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
        }
    }

    /// <summary>
    /// Has items with same valuew
    /// </summary>
    public bool HasItemsWithSameValue
    {
        get => _hasItemsWithSameValue;
        set
        {
            if (_hasItemsWithSameValue == value)
                return;
            _hasItemsWithSameValue = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Search items with duplicate names or values
    /// </summary>
    public ICommand SearchWithDuplicateNamesOrValuesCommand => new RelayCommand(() => SearchString = "*");

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }

    private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (var item in e.NewItems.OfType<Item>())
            {
                item.ValidateInParent += ItemOnValidateInParent;
            }
        }
    
        Validate();
    }

    private void ItemOnValidateInParent(object sender, EventArgs e)
    {
        Validate();
    }

    public void Validate()
    {
        foreach (var item in Items)
        {
            item.Validate();
        }

        foreach (var item in Items)
        {
            item.HasDuplicateName = false;
        }

        foreach (var group in Items
                     .GroupBy(i => i.Name)
                     .Where(g => g.Count() > 1))
        {
            foreach (var item in group)
            {
                item.HasDuplicateName = true;
            }
        }

        HasItemsWithSameValue = false;

        foreach (var item in Items)
        {
            item.HasDuplicateValue = false;
        }

        foreach (var group in Items
                     .Where(i =>
                         //// Если узел помечен на удаление, то не учитываем его
                         string.IsNullOrEmpty(i.Comment) &&
                         i.Values.ContainsKey("ru-RU"))
                     .GroupBy(i => i.Values["ru-RU"].Value)
                     .Where(g => g.Count() > 1))
        {
            foreach (var item in group)
            {
                item.HasDuplicateValue = true;
                HasItemsWithSameValue = true;
            }
        }

        HasIncorrectData = Items.Any(i => i.IsVisibleError);
    }

    private void Search()
    {
        if (string.IsNullOrEmpty(SearchString))
        {
            foreach (var item in Items)
            {
                item.IsVisible = true;
            }
        }
        else if (SearchString == "*")
        {
            foreach (var item in Items)
            {
                item.IsVisible = item.IsVisibleWarning || item.IsVisibleError;
            }
        }
        else
        {
            foreach (var item in Items)
            {
                var isVisible = false;
                foreach (var pair in item.Values)
                {
                    if (pair.Value.Value.IndexOf(SearchString, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        isVisible = true;
                        break;
                    }
                }

                item.IsVisible = isVisible;
            }
        }
    }
}