using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Julien.Avalonia.DataGrid.Models;

/// <summary>
/// Manages data source with sorting, filtering, grouping, and virtualization support.
/// </summary>
public class GridDataSource : INotifyPropertyChanged
{
    private IEnumerable? _source;
    private IList<object>? _sortedFilteredItems;
    private readonly List<SortDescriptor> _sortDescriptors = new();
    private readonly List<GroupDescriptor> _groupDescriptors = new();
    private readonly FilterGroup _filterGroup = new();
    private bool _isRefreshing;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? DataChanged;

    /// <summary>
    /// The original data source.
    /// </summary>
    public IEnumerable? Source
    {
        get => _source;
        set
        {
            if (_source is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= OnSourceCollectionChanged;
            }

            _source = value;

            if (_source is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += OnSourceCollectionChanged;
            }

            Refresh();
            OnPropertyChanged(nameof(Source));
        }
    }

    /// <summary>
    /// The processed view (sorted, filtered, grouped).
    /// </summary>
    public IReadOnlyList<object> View => _sortedFilteredItems?.AsReadOnly() ?? Array.Empty<object>().AsReadOnly();

    /// <summary>
    /// The flattened list of rows for display: data items when ungrouped, or an
    /// interleaved sequence of <see cref="GridGroup"/> headers and their data
    /// items (honoring expand/collapse) when grouped. This is what the grid binds
    /// its virtualized rows presenter to.
    /// </summary>
    public IReadOnlyList<object> VisualRows => _visualRows;
    private IReadOnlyList<object> _visualRows = Array.Empty<object>();

    /// <summary>
    /// Groups when grouping is applied.
    /// </summary>
    public ObservableCollection<GridGroup> Groups { get; } = new();

    /// <summary>
    /// Active sort descriptors.
    /// </summary>
    public IReadOnlyList<SortDescriptor> SortDescriptors => _sortDescriptors.AsReadOnly();

    /// <summary>
    /// Active group descriptors.
    /// </summary>
    public IReadOnlyList<GroupDescriptor> GroupDescriptors => _groupDescriptors.AsReadOnly();

    /// <summary>
    /// The filter group for all filters.
    /// </summary>
    public FilterGroup Filters => _filterGroup;

    /// <summary>
    /// Total count of items (before filtering).
    /// </summary>
    public int TotalCount => _source?.Cast<object>().Count() ?? 0;

    /// <summary>
    /// Count of items after filtering.
    /// </summary>
    public int FilteredCount => _sortedFilteredItems?.Count ?? 0;

    /// <summary>
    /// Whether grouping is active.
    /// </summary>
    public bool IsGrouped => _groupDescriptors.Count > 0;

    #region Sorting

    /// <summary>
    /// Adds or updates a sort descriptor.
    /// </summary>
    public void AddSort(string fieldName, ListSortDirection direction, bool append = false)
    {
        if (!append)
        {
            _sortDescriptors.Clear();
        }

        var existing = _sortDescriptors.FirstOrDefault(s => s.FieldName == fieldName);
        if (existing != null)
        {
            existing.Direction = direction;
        }
        else
        {
            _sortDescriptors.Add(new SortDescriptor
            {
                FieldName = fieldName,
                Direction = direction
            });
        }

        Refresh();
    }

    /// <summary>
    /// Toggles sort direction for a field.
    /// </summary>
    public void ToggleSort(string fieldName, bool append = false)
    {
        var existing = _sortDescriptors.FirstOrDefault(s => s.FieldName == fieldName);
        
        if (existing != null)
        {
            if (existing.Direction == ListSortDirection.Ascending)
            {
                existing.Direction = ListSortDirection.Descending;
            }
            else
            {
                _sortDescriptors.Remove(existing);
            }
        }
        else
        {
            if (!append)
            {
                _sortDescriptors.Clear();
            }
            _sortDescriptors.Add(new SortDescriptor
            {
                FieldName = fieldName,
                Direction = ListSortDirection.Ascending
            });
        }

        Refresh();
    }

    /// <summary>
    /// Clears all sorting.
    /// </summary>
    public void ClearSort()
    {
        _sortDescriptors.Clear();
        Refresh();
    }

    #endregion

    #region Filtering

    /// <summary>
    /// Adds a filter.
    /// </summary>
    public void AddFilter(GridFilter filter)
    {
        // Remove existing filter for same field
        var existing = _filterGroup.Filters.FirstOrDefault(f => f.FieldName == filter.FieldName);
        if (existing != null)
        {
            _filterGroup.Filters.Remove(existing);
        }

        _filterGroup.Filters.Add(filter);
        Refresh();
    }

    /// <summary>
    /// Removes filter for a field.
    /// </summary>
    public void RemoveFilter(string fieldName)
    {
        var filter = _filterGroup.Filters.FirstOrDefault(f => f.FieldName == fieldName);
        if (filter != null)
        {
            _filterGroup.Filters.Remove(filter);
            Refresh();
        }
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    public void ClearFilters()
    {
        _filterGroup.Filters.Clear();
        _filterGroup.SubGroups.Clear();
        Refresh();
    }

    #endregion

    #region Grouping

    /// <summary>
    /// Adds a grouping level.
    /// </summary>
    public void AddGroup(string fieldName, ListSortDirection sortDirection = ListSortDirection.Ascending)
    {
        if (_groupDescriptors.Any(g => g.FieldName == fieldName))
            return;

        _groupDescriptors.Add(new GroupDescriptor
        {
            FieldName = fieldName,
            SortDirection = sortDirection
        });

        Refresh();
    }

    /// <summary>
    /// Removes a grouping level.
    /// </summary>
    public void RemoveGroup(string fieldName)
    {
        var descriptor = _groupDescriptors.FirstOrDefault(g => g.FieldName == fieldName);
        if (descriptor != null)
        {
            _groupDescriptors.Remove(descriptor);
            Refresh();
        }
    }

    /// <summary>
    /// Clears all grouping.
    /// </summary>
    public void ClearGroups()
    {
        _groupDescriptors.Clear();
        Refresh();
    }

    #endregion

    #region Data Processing

    /// <summary>
    /// Refreshes the view by reapplying sort, filter, and grouping.
    /// </summary>
    public void Refresh()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;

        try
        {
            if (_source == null)
            {
                _sortedFilteredItems = new List<object>();
                Groups.Clear();
                _visualRows = Array.Empty<object>();
                return;
            }

            // Start with all items
            var items = _source.Cast<object>();

            // Apply filtering
            if (_filterGroup.Filters.Count > 0 || _filterGroup.SubGroups.Count > 0)
            {
                items = items.Where(item => _filterGroup.Matches(item));
            }

            // Apply sorting
            items = ApplySorting(items);

            _sortedFilteredItems = items.ToList();

            // Apply grouping
            if (_groupDescriptors.Count > 0)
            {
                BuildGroups(_sortedFilteredItems);
            }
            else
            {
                Groups.Clear();
            }

            BuildVisualRows();

            OnPropertyChanged(nameof(View));
            OnPropertyChanged(nameof(VisualRows));
            OnPropertyChanged(nameof(FilteredCount));
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private IEnumerable<object> ApplySorting(IEnumerable<object> items)
    {
        if (_sortDescriptors.Count == 0)
            return items;

        IOrderedEnumerable<object>? ordered = null;

        foreach (var descriptor in _sortDescriptors)
        {
            var first = items.FirstOrDefault();
            var rootType = first?.GetType();
            var rootGetter = rootType != null
                ? Helpers.PropertyAccessor.GetGetter(rootType, descriptor.FieldName)
                : null;
            if (first != null && rootGetter == null)
                continue;

            var fieldName = descriptor.FieldName;
            // Fast path for homogeneous collections (the common case): use the
            // pre-resolved getter directly; fall back to per-type resolution only
            // when an element's runtime type differs (polymorphic collections).
            Func<object, object?> keySelector = item =>
                item.GetType() == rootType
                    ? rootGetter!(item)
                    : Helpers.PropertyAccessor.GetValue(item, fieldName);

            if (ordered == null)
            {
                ordered = descriptor.Direction == ListSortDirection.Ascending
                    ? items.OrderBy(keySelector)
                    : items.OrderByDescending(keySelector);
            }
            else
            {
                ordered = descriptor.Direction == ListSortDirection.Ascending
                    ? ordered.ThenBy(keySelector)
                    : ordered.ThenByDescending(keySelector);
            }
        }

        return ordered ?? items;
    }

    private void BuildGroups(IList<object> items)
    {
        Groups.Clear();

        if (items.Count == 0 || _groupDescriptors.Count == 0)
            return;

        var rootGroups = BuildGroupLevel(items, 0, null);
        foreach (var group in rootGroups)
        {
            Groups.Add(group);
        }
    }

    private List<GridGroup> BuildGroupLevel(IEnumerable<object> items, int level, GridGroup? parent)
    {
        if (level >= _groupDescriptors.Count)
            return new List<GridGroup>();

        var descriptor = _groupDescriptors[level];
        var first = items.FirstOrDefault();

        if (first != null && descriptor.GroupKeySelector == null &&
            Helpers.PropertyAccessor.GetGetter(first.GetType(), descriptor.FieldName) == null)
            return new List<GridGroup>();

        var grouped = items.GroupBy(item =>
            descriptor.GroupKeySelector?.Invoke(item)
            ?? Helpers.PropertyAccessor.GetValue(item, descriptor.FieldName));

        var sortedGroups = descriptor.SortDirection == ListSortDirection.Ascending
            ? grouped.OrderBy(g => g.Key)
            : grouped.OrderByDescending(g => g.Key);

        var result = new List<GridGroup>();

        foreach (var grouping in sortedGroups)
        {
            var group = new GridGroup
            {
                Key = grouping.Key,
                FieldName = descriptor.FieldName,
                Level = level,
                Parent = parent,
                DisplayText = descriptor.DisplayTextFormatter?.Invoke(grouping.Key!) 
                    ?? $"{descriptor.FieldName}: {grouping.Key}"
            };

            if (level < _groupDescriptors.Count - 1)
            {
                // Build sub-groups
                var subGroups = BuildGroupLevel(grouping, level + 1, group);
                foreach (var subGroup in subGroups)
                {
                    group.SubGroups.Add(subGroup);
                }
            }
            else
            {
                // Leaf level - add items
                foreach (var item in grouping)
                {
                    group.Items.Add(item);
                }
            }

            result.Add(group);
        }

        return result;
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Refresh();
    }

    /// <summary>
    /// Builds the flattened <see cref="VisualRows"/> sequence from the current
    /// view and groups, honoring each group's expand/collapse state.
    /// </summary>
    private void BuildVisualRows()
    {
        if (_groupDescriptors.Count == 0)
        {
            _visualRows = _sortedFilteredItems is { } items
                ? items as IReadOnlyList<object> ?? items.ToList()
                : Array.Empty<object>();
            return;
        }

        var rows = new List<object>();
        foreach (var group in Groups)
        {
            AppendGroupRows(group, rows);
        }
        _visualRows = rows;
    }

    private static void AppendGroupRows(GridGroup group, List<object> rows)
    {
        rows.Add(group);
        if (!group.IsExpanded) return;

        if (group.SubGroups.Count > 0)
        {
            foreach (var subGroup in group.SubGroups)
                AppendGroupRows(subGroup, rows);
        }
        else
        {
            foreach (var item in group.Items)
                rows.Add(item);
        }
    }

    /// <summary>
    /// Rebuilds <see cref="VisualRows"/> in place (e.g. after a group is expanded
    /// or collapsed) without re-running sort/filter/group, and notifies listeners.
    /// </summary>
    public void RebuildVisualRows()
    {
        BuildVisualRows();
        OnPropertyChanged(nameof(VisualRows));
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Item Access

    /// <summary>
    /// Gets an item at the specified index in the filtered/sorted view.
    /// </summary>
    public object? GetItemAt(int index)
    {
        if (_sortedFilteredItems == null || index < 0 || index >= _sortedFilteredItems.Count)
            return null;

        return _sortedFilteredItems[index];
    }

    /// <summary>
    /// Gets the index of an item in the filtered/sorted view.
    /// </summary>
    public int IndexOf(object item)
    {
        return _sortedFilteredItems?.IndexOf(item) ?? -1;
    }

    /// <summary>
    /// Gets items for virtualized display.
    /// </summary>
    public IEnumerable<object> GetItemsInRange(int startIndex, int count)
    {
        if (_sortedFilteredItems == null)
            yield break;

        var endIndex = Math.Min(startIndex + count, _sortedFilteredItems.Count);
        for (int i = startIndex; i < endIndex; i++)
        {
            yield return _sortedFilteredItems[i];
        }
    }

    #endregion

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Describes a sort operation.
/// </summary>
public class SortDescriptor
{
    public string FieldName { get; set; } = string.Empty;
    public ListSortDirection Direction { get; set; } = ListSortDirection.Ascending;
}
