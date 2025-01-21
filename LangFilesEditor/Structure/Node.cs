namespace LangFilesEditor.Structure;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

internal class Node : ObservableObject
{
    private bool _hasIncorrectData;

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

            Validate();
        }
    }

    private void ItemOnValidateInParent(object sender, EventArgs e)
    {
        Validate();
    }

    private void Validate()
    {
        foreach (var item in Items)
        {
            item.HasDuplicate = false;
        }

        foreach (var group in Items
                     .GroupBy(i => i.Name)
                     .Where(g => g.Count() > 1))
        {
            foreach (var item in group)
            {
                item.HasDuplicate = true;
            }
        }

        HasIncorrectData = Items.Any(i => i.IsVisibleWarning);
    }
}