using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace HDev.UI.DataGrid.Models;

/// <summary>
/// Represents a group of items in the DataGrid.
/// </summary>
public class GridGroup : INotifyPropertyChanged
{
    private bool _isExpanded = true;
    private object? _key;
    private string _displayText = string.Empty;
    private string _summaryText = string.Empty;
    private int _level;
    private GridGroup? _parent;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The grouping key value.
    /// </summary>
    public object? Key
    {
        get => _key;
        set
        {
            _key = value;
            OnPropertyChanged(nameof(Key));
        }
    }

    /// <summary>
    /// The field name this group is based on.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Display text for the group header.
    /// </summary>
    public string DisplayText
    {
        get => _displayText;
        set
        {
            _displayText = value;
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    /// <summary>
    /// Computed aggregate text for the group header (e.g. "Sum: 1,200 | Count: 3").
    /// Populated by the grid when group summaries are configured.
    /// </summary>
    public string SummaryText
    {
        get => _summaryText;
        set
        {
            if (_summaryText == value) return;
            _summaryText = value;
            OnPropertyChanged(nameof(SummaryText));
        }
    }

    /// <summary>
    /// Nesting level (0 for root groups).
    /// </summary>
    public int Level
    {
        get => _level;
        set
        {
            _level = value;
            OnPropertyChanged(nameof(Level));
        }
    }

    /// <summary>
    /// Whether the group is expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            OnPropertyChanged(nameof(IsExpanded));
        }
    }

    /// <summary>
    /// Parent group for nested grouping.
    /// </summary>
    public GridGroup? Parent
    {
        get => _parent;
        set
        {
            _parent = value;
            OnPropertyChanged(nameof(Parent));
        }
    }

    /// <summary>
    /// Child groups for nested grouping.
    /// </summary>
    public ObservableCollection<GridGroup> SubGroups { get; } = new();

    /// <summary>
    /// Items in this group (only at the leaf level).
    /// </summary>
    public ObservableCollection<object> Items { get; } = new();

    /// <summary>
    /// Total item count including all subgroups.
    /// </summary>
    public int TotalItemCount
    {
        get
        {
            if (SubGroups.Count > 0)
                return SubGroups.Sum(g => g.TotalItemCount);
            return Items.Count;
        }
    }

    /// <summary>
    /// Gets all items including those in subgroups.
    /// </summary>
    public IEnumerable<object> GetAllItems()
    {
        if (SubGroups.Count > 0)
        {
            foreach (var subGroup in SubGroups)
            {
                foreach (var item in subGroup.GetAllItems())
                {
                    yield return item;
                }
            }
        }
        else
        {
            foreach (var item in Items)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Toggles the expanded state.
    /// </summary>
    public void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    /// <summary>
    /// Expands this group and all parent groups.
    /// </summary>
    public void ExpandToRoot()
    {
        IsExpanded = true;
        Parent?.ExpandToRoot();
    }

    /// <summary>
    /// Collapses all subgroups recursively.
    /// </summary>
    public void CollapseAll()
    {
        IsExpanded = false;
        foreach (var subGroup in SubGroups)
        {
            subGroup.CollapseAll();
        }
    }

    /// <summary>
    /// Expands all subgroups recursively.
    /// </summary>
    public void ExpandAll()
    {
        IsExpanded = true;
        foreach (var subGroup in SubGroups)
        {
            subGroup.ExpandAll();
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Describes how a column should be grouped.
/// </summary>
public class GroupDescriptor
{
    public string FieldName { get; set; } = string.Empty;
    public ListSortDirection SortDirection { get; set; } = ListSortDirection.Ascending;
    
    /// <summary>
    /// Optional custom grouping function.
    /// </summary>
    public Func<object, object>? GroupKeySelector { get; set; }
    
    /// <summary>
    /// Optional custom display text formatter.
    /// </summary>
    public Func<object, string>? DisplayTextFormatter { get; set; }
}
