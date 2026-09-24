using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace HDev.UI.DataGrid.Models;

/// <summary>
/// Manages selection state for the DataGrid.
/// </summary>
public class GridSelection : INotifyPropertyChanged
{
    private SelectionMode _mode = SelectionMode.Single;
    private object? _currentItem;
    private int _currentRowIndex = -1;
    private GridColumn? _currentColumn;
    private bool _isSelecting;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Currently selected items.
    /// </summary>
    public ObservableCollection<object> SelectedItems { get; } = new();

    /// <summary>
    /// Currently selected row indices.
    /// </summary>
    public ObservableCollection<int> SelectedRowIndices { get; } = new();

    /// <summary>
    /// Selected cell positions (row, column).
    /// </summary>
    public ObservableCollection<GridCellPosition> SelectedCells { get; } = new();

    /// <summary>
    /// Selection mode.
    /// </summary>
    public SelectionMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                ClearSelection();
                OnPropertyChanged(nameof(Mode));
            }
        }
    }

    /// <summary>
    /// The current focused item.
    /// </summary>
    public object? CurrentItem
    {
        get => _currentItem;
        set
        {
            if (_currentItem != value)
            {
                _currentItem = value;
                OnPropertyChanged(nameof(CurrentItem));
            }
        }
    }

    /// <summary>
    /// The current focused row index.
    /// </summary>
    public int CurrentRowIndex
    {
        get => _currentRowIndex;
        set
        {
            if (_currentRowIndex != value)
            {
                _currentRowIndex = value;
                OnPropertyChanged(nameof(CurrentRowIndex));
            }
        }
    }

    /// <summary>
    /// The current focused column.
    /// </summary>
    public GridColumn? CurrentColumn
    {
        get => _currentColumn;
        set
        {
            if (_currentColumn != value)
            {
                _currentColumn = value;
                OnPropertyChanged(nameof(CurrentColumn));
            }
        }
    }

    /// <summary>
    /// First selected item (convenience property).
    /// </summary>
    public object? SelectedItem => SelectedItems.FirstOrDefault();

    public GridSelection()
    {
        SelectedItems.CollectionChanged += OnSelectedItemsChanged;
    }

    /// <summary>
    /// Selects a single item.
    /// </summary>
    public void Select(object item, int rowIndex, bool addToSelection = false)
    {
        if (_isSelecting) return;
        _isSelecting = true;

        try
        {
            var oldSelection = SelectedItems.ToList();

            if (!addToSelection || Mode == SelectionMode.Single)
            {
                SelectedItems.Clear();
                SelectedRowIndices.Clear();
            }

            if (!SelectedItems.Contains(item))
            {
                SelectedItems.Add(item);
                SelectedRowIndices.Add(rowIndex);
            }

            CurrentItem = item;
            CurrentRowIndex = rowIndex;

            RaiseSelectionChanged(oldSelection, SelectedItems.ToList());
        }
        finally
        {
            _isSelecting = false;
        }
    }

    /// <summary>
    /// Selects a range of items.
    /// </summary>
    public void SelectRange(IEnumerable<object> items, IEnumerable<int> rowIndices)
    {
        if (_isSelecting || Mode == SelectionMode.Single) return;
        _isSelecting = true;

        try
        {
            var oldSelection = SelectedItems.ToList();
            var itemList = items.ToList();
            var indexList = rowIndices.ToList();

            foreach (var item in itemList)
            {
                if (!SelectedItems.Contains(item))
                    SelectedItems.Add(item);
            }

            foreach (var index in indexList)
            {
                if (!SelectedRowIndices.Contains(index))
                    SelectedRowIndices.Add(index);
            }

            RaiseSelectionChanged(oldSelection, SelectedItems.ToList());
        }
        finally
        {
            _isSelecting = false;
        }
    }

    /// <summary>
    /// Toggles selection of an item.
    /// </summary>
    public void Toggle(object item, int rowIndex)
    {
        if (SelectedItems.Contains(item))
            Deselect(item, rowIndex);
        else
            Select(item, rowIndex, Mode == SelectionMode.Multiple);
    }

    /// <summary>
    /// Deselects a single item.
    /// </summary>
    public void Deselect(object item, int rowIndex)
    {
        if (_isSelecting) return;
        _isSelecting = true;

        try
        {
            var oldSelection = SelectedItems.ToList();

            SelectedItems.Remove(item);
            SelectedRowIndices.Remove(rowIndex);

            if (CurrentItem == item)
            {
                CurrentItem = SelectedItems.FirstOrDefault();
                CurrentRowIndex = SelectedRowIndices.FirstOrDefault();
            }

            RaiseSelectionChanged(oldSelection, SelectedItems.ToList());
        }
        finally
        {
            _isSelecting = false;
        }
    }

    /// <summary>
    /// Selects a cell.
    /// </summary>
    public void SelectCell(int rowIndex, GridColumn column, bool addToSelection = false)
    {
        if (Mode != SelectionMode.Cell) return;

        if (!addToSelection)
        {
            SelectedCells.Clear();
        }

        var position = new GridCellPosition(rowIndex, column);
        if (!SelectedCells.Contains(position))
        {
            SelectedCells.Add(position);
        }

        CurrentRowIndex = rowIndex;
        CurrentColumn = column;
    }

    /// <summary>
    /// Checks if an item is selected.
    /// </summary>
    public bool IsSelected(object item) => SelectedItems.Contains(item);

    /// <summary>
    /// Checks if a row is selected.
    /// </summary>
    public bool IsRowSelected(int rowIndex) => SelectedRowIndices.Contains(rowIndex);

    /// <summary>
    /// Checks if a cell is selected.
    /// </summary>
    public bool IsCellSelected(int rowIndex, GridColumn column)
    {
        return SelectedCells.Any(c => c.RowIndex == rowIndex && c.Column == column);
    }

    /// <summary>
    /// Clears all selection.
    /// </summary>
    public void ClearSelection()
    {
        if (_isSelecting) return;
        _isSelecting = true;

        try
        {
            var oldSelection = SelectedItems.ToList();

            SelectedItems.Clear();
            SelectedRowIndices.Clear();
            SelectedCells.Clear();
            CurrentItem = null;
            CurrentRowIndex = -1;
            CurrentColumn = null;

            if (oldSelection.Count > 0)
            {
                RaiseSelectionChanged(oldSelection, new List<object>());
            }
        }
        finally
        {
            _isSelecting = false;
        }
    }

    /// <summary>
    /// Selects all items.
    /// </summary>
    public void SelectAll(IEnumerable<object> allItems)
    {
        if (Mode == SelectionMode.Single) return;

        _isSelecting = true;
        try
        {
            var oldSelection = SelectedItems.ToList();
            var itemList = allItems.ToList();

            SelectedItems.Clear();
            SelectedRowIndices.Clear();

            for (int i = 0; i < itemList.Count; i++)
            {
                SelectedItems.Add(itemList[i]);
                SelectedRowIndices.Add(i);
            }

            RaiseSelectionChanged(oldSelection, SelectedItems.ToList());
        }
        finally
        {
            _isSelecting = false;
        }
    }

    private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SelectedItem));
    }

    private void RaiseSelectionChanged(IList<object> oldItems, IList<object> newItems)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldItems, newItems));
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Selection modes for the DataGrid.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// No selection allowed.
    /// </summary>
    None,
    
    /// <summary>
    /// Single row selection.
    /// </summary>
    Single,
    
    /// <summary>
    /// Multiple row selection.
    /// </summary>
    Multiple,
    
    /// <summary>
    /// Cell-level selection.
    /// </summary>
    Cell
}

/// <summary>
/// Represents a cell position in the grid.
/// </summary>
public readonly struct GridCellPosition : IEquatable<GridCellPosition>
{
    public int RowIndex { get; }
    public GridColumn Column { get; }

    public GridCellPosition(int rowIndex, GridColumn column)
    {
        RowIndex = rowIndex;
        Column = column;
    }

    public bool Equals(GridCellPosition other) =>
        RowIndex == other.RowIndex && Column == other.Column;

    public override bool Equals(object? obj) =>
        obj is GridCellPosition other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(RowIndex, Column);

    public static bool operator ==(GridCellPosition left, GridCellPosition right) =>
        left.Equals(right);

    public static bool operator !=(GridCellPosition left, GridCellPosition right) =>
        !left.Equals(right);
}

/// <summary>
/// Event args for selection changes.
/// </summary>
public class SelectionChangedEventArgs : EventArgs
{
    public IList<object> RemovedItems { get; }
    public IList<object> AddedItems { get; }

    public SelectionChangedEventArgs(IList<object> removedItems, IList<object> addedItems)
    {
        RemovedItems = removedItems;
        AddedItems = addedItems;
    }
}
