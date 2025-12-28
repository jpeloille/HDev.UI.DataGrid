using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Julien.Avalonia.DataGrid.Models;
using Models = Julien.Avalonia.DataGrid.Models;
using SelectionMode = Julien.Avalonia.DataGrid.Models.SelectionMode;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace Julien.Avalonia.DataGrid.Controls;

/// <summary>
/// A high-performance, feature-rich DataGrid control for Avalonia.
/// Supports sorting, filtering, grouping, editing, virtualization, and more.
/// </summary>
public class JDataGrid : TemplatedControl
{
    #region Private Fields

    private GridDataSource _dataSource = new();
    private GridSelection _selection = new();
    private ScrollViewer? _scrollViewer;
    private ScrollViewer? _headerScrollViewer;
    private ScrollViewer? _filterScrollViewer;
    private ItemsControl? _rowsPresenter;
    private ItemsControl? _headerPresenter;
    private ItemsControl? _frozenHeaderPresenter;
    private ItemsControl? _filterRowPresenter;
    private ItemsControl? _frozenFilterPresenter;
    private JDataGridGroupPanel? _groupPanel;
    private Border? _frozenHeaderSeparator;
    private Border? _frozenFilterSeparator;
    private object? _editingItem;
    private GridColumn? _editingColumn;

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<JDataGrid, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<GridColumnCollection> ColumnsProperty =
        AvaloniaProperty.Register<JDataGrid, GridColumnCollection>(nameof(Columns));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<JDataGrid, object?>(nameof(SelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<JDataGrid, IList?>(nameof(SelectedItems));

    public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<JDataGrid, SelectionMode>(nameof(SelectionMode), SelectionMode.Single);

    public static readonly StyledProperty<double> RowHeightProperty =
        AvaloniaProperty.Register<JDataGrid, double>(nameof(RowHeight), 36);

    public static readonly StyledProperty<double> HeaderHeightProperty =
        AvaloniaProperty.Register<JDataGrid, double>(nameof(HeaderHeight), 40);

    public static readonly StyledProperty<bool> ShowHeaderProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(ShowHeader), true);

    public static readonly StyledProperty<bool> ShowFilterRowProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(ShowFilterRow), false);

    public static readonly StyledProperty<bool> ShowGroupPanelProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(ShowGroupPanel), false);

    public static readonly StyledProperty<bool> AllowSortingProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(AllowSorting), true);

    public static readonly StyledProperty<bool> AllowFilteringProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(AllowFiltering), true);

    public static readonly StyledProperty<bool> AllowGroupingProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(AllowGrouping), true);

    public static readonly StyledProperty<bool> AllowEditingProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(AllowEditing), true);

    public static readonly StyledProperty<bool> AllowColumnResizingProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(AllowColumnResizing), true);

    public static readonly StyledProperty<bool> AllowColumnReorderingProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(AllowColumnReordering), true);

    public static readonly StyledProperty<bool> AutoGenerateColumnsProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(AutoGenerateColumns), true);

    public static readonly StyledProperty<bool> IsVirtualizingProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(IsVirtualizing), true);

    public static readonly StyledProperty<bool> ShowRowNumbersProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(ShowRowNumbers), false);

    public static readonly StyledProperty<bool> ShowRowIndicatorProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(ShowRowIndicator), true);

    public static readonly StyledProperty<IBrush?> AlternateRowBackgroundProperty =
        AvaloniaProperty.Register<JDataGrid, IBrush?>(nameof(AlternateRowBackground));

    public static readonly StyledProperty<IBrush?> SelectedRowBackgroundProperty =
        AvaloniaProperty.Register<JDataGrid, IBrush?>(nameof(SelectedRowBackground));

    public static readonly StyledProperty<IBrush?> HoverRowBackgroundProperty =
        AvaloniaProperty.Register<JDataGrid, IBrush?>(nameof(HoverRowBackground));

    public static readonly StyledProperty<IBrush?> GridLinesColorProperty =
        AvaloniaProperty.Register<JDataGrid, IBrush?>(nameof(GridLinesColor));

    public static readonly StyledProperty<GridLinesVisibility> GridLinesVisibilityProperty =
        AvaloniaProperty.Register<JDataGrid, GridLinesVisibility>(nameof(GridLinesVisibility), GridLinesVisibility.Horizontal);

    public static readonly StyledProperty<int> FrozenColumnCountProperty =
        AvaloniaProperty.Register<JDataGrid, int>(nameof(FrozenColumnCount), 0);

    public static readonly StyledProperty<string?> EmptyContentProperty =
        AvaloniaProperty.Register<JDataGrid, string?>(nameof(EmptyContent), "No data to display");

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(IsLoading), false);

    public static readonly StyledProperty<string?> LoadingTextProperty =
        AvaloniaProperty.Register<JDataGrid, string?>(nameof(LoadingText), "Loading...");

    #endregion

    #region Command Properties

    public static readonly StyledProperty<ICommand?> RowDoubleClickCommandProperty =
        AvaloniaProperty.Register<JDataGrid, ICommand?>(nameof(RowDoubleClickCommand));

    public static readonly StyledProperty<ICommand?> SelectionChangedCommandProperty =
        AvaloniaProperty.Register<JDataGrid, ICommand?>(nameof(SelectionChangedCommand));

    public static readonly StyledProperty<ICommand?> CellEditEndingCommandProperty =
        AvaloniaProperty.Register<JDataGrid, ICommand?>(nameof(CellEditEndingCommand));

    #endregion

    #region Routed Events

    public static readonly RoutedEvent<global::Avalonia.Controls.SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<JDataGrid, global::Avalonia.Controls.SelectionChangedEventArgs>(
            nameof(SelectionChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<CellEditEventArgs> CellEditStartingEvent =
        RoutedEvent.Register<JDataGrid, CellEditEventArgs>(
            nameof(CellEditStarting), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<CellEditEventArgs> CellEditEndingEvent =
        RoutedEvent.Register<JDataGrid, CellEditEventArgs>(
            nameof(CellEditEnding), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ColumnEventArgs> ColumnSortedEvent =
        RoutedEvent.Register<JDataGrid, ColumnEventArgs>(
            nameof(ColumnSorted), RoutingStrategies.Bubble);

    public event EventHandler<global::Avalonia.Controls.SelectionChangedEventArgs>? SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    public event EventHandler<CellEditEventArgs>? CellEditStarting
    {
        add => AddHandler(CellEditStartingEvent, value);
        remove => RemoveHandler(CellEditStartingEvent, value);
    }

    public event EventHandler<CellEditEventArgs>? CellEditEnding
    {
        add => AddHandler(CellEditEndingEvent, value);
        remove => RemoveHandler(CellEditEndingEvent, value);
    }

    public event EventHandler<ColumnEventArgs>? ColumnSorted
    {
        add => AddHandler(ColumnSortedEvent, value);
        remove => RemoveHandler(ColumnSortedEvent, value);
    }

    #endregion

    #region Properties

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public GridColumnCollection Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public IList? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    public SelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public double RowHeight
    {
        get => GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public double HeaderHeight
    {
        get => GetValue(HeaderHeightProperty);
        set => SetValue(HeaderHeightProperty, value);
    }

    public bool ShowHeader
    {
        get => GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public bool ShowFilterRow
    {
        get => GetValue(ShowFilterRowProperty);
        set => SetValue(ShowFilterRowProperty, value);
    }

    public bool ShowGroupPanel
    {
        get => GetValue(ShowGroupPanelProperty);
        set => SetValue(ShowGroupPanelProperty, value);
    }

    public bool AllowSorting
    {
        get => GetValue(AllowSortingProperty);
        set => SetValue(AllowSortingProperty, value);
    }

    public bool AllowFiltering
    {
        get => GetValue(AllowFilteringProperty);
        set => SetValue(AllowFilteringProperty, value);
    }

    public bool AllowGrouping
    {
        get => GetValue(AllowGroupingProperty);
        set => SetValue(AllowGroupingProperty, value);
    }

    public bool AllowEditing
    {
        get => GetValue(AllowEditingProperty);
        set => SetValue(AllowEditingProperty, value);
    }

    public bool AllowColumnResizing
    {
        get => GetValue(AllowColumnResizingProperty);
        set => SetValue(AllowColumnResizingProperty, value);
    }

    public bool AllowColumnReordering
    {
        get => GetValue(AllowColumnReorderingProperty);
        set => SetValue(AllowColumnReorderingProperty, value);
    }

    public bool AutoGenerateColumns
    {
        get => GetValue(AutoGenerateColumnsProperty);
        set => SetValue(AutoGenerateColumnsProperty, value);
    }

    public bool IsVirtualizing
    {
        get => GetValue(IsVirtualizingProperty);
        set => SetValue(IsVirtualizingProperty, value);
    }

    public bool ShowRowNumbers
    {
        get => GetValue(ShowRowNumbersProperty);
        set => SetValue(ShowRowNumbersProperty, value);
    }

    public bool ShowRowIndicator
    {
        get => GetValue(ShowRowIndicatorProperty);
        set => SetValue(ShowRowIndicatorProperty, value);
    }

    public IBrush? AlternateRowBackground
    {
        get => GetValue(AlternateRowBackgroundProperty);
        set => SetValue(AlternateRowBackgroundProperty, value);
    }

    public IBrush? SelectedRowBackground
    {
        get => GetValue(SelectedRowBackgroundProperty);
        set => SetValue(SelectedRowBackgroundProperty, value);
    }

    public IBrush? HoverRowBackground
    {
        get => GetValue(HoverRowBackgroundProperty);
        set => SetValue(HoverRowBackgroundProperty, value);
    }

    public IBrush? GridLinesColor
    {
        get => GetValue(GridLinesColorProperty);
        set => SetValue(GridLinesColorProperty, value);
    }

    public GridLinesVisibility GridLinesVisibility
    {
        get => GetValue(GridLinesVisibilityProperty);
        set => SetValue(GridLinesVisibilityProperty, value);
    }

    public int FrozenColumnCount
    {
        get => GetValue(FrozenColumnCountProperty);
        set => SetValue(FrozenColumnCountProperty, value);
    }

    public string? EmptyContent
    {
        get => GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    public string? LoadingText
    {
        get => GetValue(LoadingTextProperty);
        set => SetValue(LoadingTextProperty, value);
    }

    public ICommand? RowDoubleClickCommand
    {
        get => GetValue(RowDoubleClickCommandProperty);
        set => SetValue(RowDoubleClickCommandProperty, value);
    }

    public ICommand? SelectionChangedCommand
    {
        get => GetValue(SelectionChangedCommandProperty);
        set => SetValue(SelectionChangedCommandProperty, value);
    }

    public ICommand? CellEditEndingCommand
    {
        get => GetValue(CellEditEndingCommandProperty);
        set => SetValue(CellEditEndingCommandProperty, value);
    }

    /// <summary>
    /// Gets the internal data source for advanced operations.
    /// </summary>
    public GridDataSource DataSource => _dataSource;

    /// <summary>
    /// Gets the selection manager.
    /// </summary>
    public GridSelection Selection => _selection;

    /// <summary>
    /// Gets the current view (filtered/sorted items).
    /// </summary>
    public IReadOnlyList<object> View => _dataSource.View;

    /// <summary>
    /// Gets the groups when grouping is active.
    /// </summary>
    public IReadOnlyList<GridGroup> Groups => _dataSource.Groups;

    /// <summary>
    /// Whether data is grouped.
    /// </summary>
    public bool IsGrouped => _dataSource.IsGrouped;

    #endregion

    #region Constructor

    public JDataGrid()
    {
        Columns = new GridColumnCollection();
        _selection.SelectionChanged += OnSelectionManagerChanged;
        _dataSource.DataChanged += OnDataSourceChanged;
    }

    static JDataGrid()
    {
        ItemsSourceProperty.Changed.AddClassHandler<JDataGrid>((grid, e) => grid.OnItemsSourceChanged(e));
        SelectionModeProperty.Changed.AddClassHandler<JDataGrid>((grid, e) => grid.OnSelectionModeChanged(e));
        SelectedItemProperty.Changed.AddClassHandler<JDataGrid>((grid, e) => grid.OnSelectedItemChanged(e));
    }

    #endregion

    #region Template Methods

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _headerScrollViewer = e.NameScope.Find<ScrollViewer>("PART_HeaderScrollViewer");
        _filterScrollViewer = e.NameScope.Find<ScrollViewer>("PART_FilterScrollViewer");
        _rowsPresenter = e.NameScope.Find<ItemsControl>("PART_RowsPresenter");
        _headerPresenter = e.NameScope.Find<ItemsControl>("PART_HeaderPresenter");
        _frozenHeaderPresenter = e.NameScope.Find<ItemsControl>("PART_FrozenHeaderPresenter");
        _filterRowPresenter = e.NameScope.Find<ItemsControl>("PART_FilterRowPresenter");
        _frozenFilterPresenter = e.NameScope.Find<ItemsControl>("PART_FrozenFilterPresenter");
        _groupPanel = e.NameScope.Find<JDataGridGroupPanel>("PART_GroupPanel");
        _frozenHeaderSeparator = e.NameScope.Find<Border>("PART_FrozenHeaderSeparator");
        _frozenFilterSeparator = e.NameScope.Find<Border>("PART_FrozenFilterSeparator");

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }

        // Listen to column header events (bubbled from JDataGridColumnHeader)
        AddHandler(JDataGridColumnHeader.SortRequestedEvent, OnColumnSortRequested);
        AddHandler(JDataGridColumnHeader.ResizeCompletedEvent, OnColumnResizeCompleted);
        AddHandler(JDataGridColumnHeader.ReorderCompletedEvent, OnColumnReorderCompleted);

        // Listen to filter cell events (bubbled from JDataGridFilterCell)
        AddHandler(JDataGridFilterCell.FilterChangedEvent, OnFilterChanged);

        // Listen to group panel events
        AddHandler(JDataGridGroupPanel.ColumnGroupedEvent, OnColumnGrouped);
        AddHandler(JDataGridGroupPanel.ColumnUngroupedEvent, OnColumnUngrouped);

        RefreshView();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sorts by a column.
    /// </summary>
    public void SortBy(string fieldName, ListSortDirection? direction = null, bool append = false)
    {
        if (!AllowSorting) return;

        var column = Columns.GetByFieldName(fieldName);
        if (column == null || !column.AllowSort) return;

        if (direction.HasValue)
        {
            _dataSource.AddSort(fieldName, direction.Value, append);
            column.SortDirection = direction.Value;
        }
        else
        {
            _dataSource.ToggleSort(fieldName, append);
            var sort = _dataSource.SortDescriptors.FirstOrDefault(s => s.FieldName == fieldName);
            column.SortDirection = sort?.Direction;
        }

        UpdateColumnSortIndicators();
        RaiseEvent(new ColumnEventArgs(ColumnSortedEvent, column));
    }

    /// <summary>
    /// Clears all sorting.
    /// </summary>
    public void ClearSort()
    {
        _dataSource.ClearSort();
        foreach (var column in Columns)
        {
            column.SortDirection = null;
            column.SortIndex = -1;
        }
    }

    /// <summary>
    /// Adds a filter to a column.
    /// </summary>
    public void Filter(string fieldName, FilterOperator op, object? value)
    {
        if (!AllowFiltering) return;

        var column = Columns.GetByFieldName(fieldName);
        if (column == null || !column.AllowFilter) return;

        _dataSource.AddFilter(new GridFilter
        {
            FieldName = fieldName,
            Operator = op,
            Value = value
        });
    }

    /// <summary>
    /// Clears filter for a column.
    /// </summary>
    public void ClearFilter(string fieldName)
    {
        _dataSource.RemoveFilter(fieldName);
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    public void ClearAllFilters()
    {
        _dataSource.ClearFilters();
    }

    /// <summary>
    /// Groups by a column.
    /// </summary>
    public void GroupBy(string fieldName, ListSortDirection sortDirection = ListSortDirection.Ascending)
    {
        if (!AllowGrouping) return;

        var column = Columns.GetByFieldName(fieldName);
        if (column == null || !column.AllowGroup) return;

        _dataSource.AddGroup(fieldName, sortDirection);
    }

    /// <summary>
    /// Removes grouping for a column.
    /// </summary>
    public void Ungroup(string fieldName)
    {
        _dataSource.RemoveGroup(fieldName);
    }

    /// <summary>
    /// Clears all grouping.
    /// </summary>
    public void ClearGrouping()
    {
        _dataSource.ClearGroups();
    }

    /// <summary>
    /// Begins editing a cell.
    /// </summary>
    public void BeginEdit(object item, GridColumn column)
    {
        if (!AllowEditing || column.IsReadOnly) return;

        var args = new CellEditEventArgs(CellEditStartingEvent, item, column);
        RaiseEvent(args);

        if (args.Cancel) return;

        _editingItem = item;
        _editingColumn = column;

        // TODO: Swap cell template to edit template
    }

    /// <summary>
    /// Commits the current edit.
    /// </summary>
    public void CommitEdit()
    {
        if (_editingItem == null || _editingColumn == null) return;

        var args = new CellEditEventArgs(CellEditEndingEvent, _editingItem, _editingColumn);
        RaiseEvent(args);

        if (!args.Cancel)
        {
            CellEditEndingCommand?.Execute(args);
        }

        _editingItem = null;
        _editingColumn = null;
    }

    /// <summary>
    /// Cancels the current edit.
    /// </summary>
    public void CancelEdit()
    {
        _editingItem = null;
        _editingColumn = null;
        // TODO: Restore original value
    }

    /// <summary>
    /// Scrolls to make an item visible.
    /// </summary>
    public void ScrollIntoView(object item)
    {
        var index = _dataSource.IndexOf(item);
        if (index < 0) return;

        var offset = index * RowHeight;
        if (_scrollViewer != null)
        {
            _scrollViewer.Offset = new global::Avalonia.Vector(_scrollViewer.Offset.X, offset);
        }
    }

    /// <summary>
    /// Refreshes the data view.
    /// </summary>
    public void RefreshData()
    {
        _dataSource.Refresh();
    }

    /// <summary>
    /// Expands all groups.
    /// </summary>
    public void ExpandAllGroups()
    {
        foreach (var group in _dataSource.Groups)
        {
            group.ExpandAll();
        }
    }

    /// <summary>
    /// Collapses all groups.
    /// </summary>
    public void CollapseAllGroups()
    {
        foreach (var group in _dataSource.Groups)
        {
            group.CollapseAll();
        }
    }

    #endregion

    #region Private Methods

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnSourceCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += OnSourceCollectionChanged;
        }

        _dataSource.Source = e.NewValue as IEnumerable;

        if (AutoGenerateColumns && Columns.Count == 0)
        {
            GenerateColumns();
        }

        _selection.ClearSelection();
        RefreshView();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _dataSource.Refresh();
    }

    private void GenerateColumns()
    {
        Columns.Clear();

        var firstItem = ItemsSource?.Cast<object>().FirstOrDefault();
        if (firstItem == null) return;

        var properties = firstItem.GetType().GetProperties()
            .Where(p => p.CanRead && IsDisplayableType(p.PropertyType));

        foreach (var prop in properties)
        {
            var column = new GridColumn
            {
                FieldName = prop.Name,
                Header = FormatPropertyName(prop.Name),
                ColumnType = GetColumnType(prop.PropertyType),
                IsReadOnly = !prop.CanWrite,
                Width = new GridLength(1, GridUnitType.Star)
            };

            Columns.Add(column);
        }

        Columns.UpdateVisibleIndices();
    }

    private bool IsDisplayableType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType.IsPrimitive ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(Guid) ||
               underlyingType.IsEnum;
    }

    private ColumnType GetColumnType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(bool)) return ColumnType.Boolean;
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset)) return ColumnType.DateTime;
        if (underlyingType.IsEnum) return ColumnType.ComboBox;
        if (IsNumericType(underlyingType)) return ColumnType.Numeric;

        return ColumnType.Text;
    }

    private bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    private string FormatPropertyName(string name)
    {
        // Insert spaces before capitals: "FirstName" -> "First Name"
        return string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }

    private void OnSelectionModeChanged(AvaloniaPropertyChangedEventArgs e)
    {
        _selection.Mode = (SelectionMode)e.NewValue!;
    }

    private void OnSelectedItemChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue != null && !_selection.IsSelected(e.NewValue))
        {
            var index = _dataSource.IndexOf(e.NewValue);
            if (index >= 0)
            {
                _selection.Select(e.NewValue, index);
            }
        }
    }

    private void OnSelectionManagerChanged(object? sender, Models.SelectionChangedEventArgs e)
    {
        SetCurrentValue(SelectedItemProperty, _selection.SelectedItem);
        SetCurrentValue(SelectedItemsProperty, _selection.SelectedItems);

        RaiseEvent(new global::Avalonia.Controls.SelectionChangedEventArgs(SelectionChangedEvent, (System.Collections.IList)e.RemovedItems, (System.Collections.IList)e.AddedItems));
        SelectionChangedCommand?.Execute(_selection.SelectedItem);
    }

    private void OnDataSourceChanged(object? sender, EventArgs e)
    {
        RefreshView();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Synchronize horizontal scroll between headers, filter row, and data rows
        if (_scrollViewer != null)
        {
            var horizontalOffset = _scrollViewer.Offset.X;

            if (_headerScrollViewer != null)
            {
                _headerScrollViewer.Offset = new global::Avalonia.Vector(horizontalOffset, _headerScrollViewer.Offset.Y);
            }

            if (_filterScrollViewer != null)
            {
                _filterScrollViewer.Offset = new global::Avalonia.Vector(horizontalOffset, _filterScrollViewer.Offset.Y);
            }
        }
    }

    private void OnColumnSortRequested(object? sender, ColumnEventArgs e)
    {
        if (!AllowSorting || e.Column == null) return;

        _dataSource.ToggleSort(e.Column.FieldName, append: false);
        UpdateColumnSortIndicators();

        e.Handled = true;
    }

    private void OnColumnResizeCompleted(object? sender, ColumnResizeEventArgs e)
    {
        // Column resize is already handled in the header
        e.Handled = true;
    }

    private void OnColumnReorderCompleted(object? sender, ColumnReorderEventArgs e)
    {
        if (!AllowColumnReordering) return;

        Columns.MoveColumn(e.Column, e.NewIndex);
        RefreshView();
        e.Handled = true;
    }

    private void OnFilterChanged(object? sender, FilterChangedEventArgs e)
    {
        if (!AllowFiltering || e.Column == null) return;

        if (string.IsNullOrWhiteSpace(e.FilterText))
        {
            // Remove filter for this column
            _dataSource.RemoveFilter(e.Column.FieldName);
        }
        else
        {
            // Add or update filter for this column
            var filter = new Models.GridFilter
            {
                FieldName = e.Column.FieldName,
                Operator = Models.FilterOperator.Contains,
                Value = e.FilterText,
                IsCaseSensitive = false
            };
            _dataSource.AddFilter(filter);
        }

        e.Handled = true;
    }

    private void OnColumnGrouped(object? sender, GroupColumnEventArgs e)
    {
        if (!AllowGrouping || e.Column == null) return;

        _dataSource.AddGroup(e.Column.FieldName);
        e.Handled = true;
    }

    private void OnColumnUngrouped(object? sender, GroupColumnEventArgs e)
    {
        if (e.Column == null) return;

        _dataSource.RemoveGroup(e.Column.FieldName);
        e.Handled = true;
    }

    private void UpdateColumnSortIndicators()
    {
        for (int i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            var sort = _dataSource.SortDescriptors.FirstOrDefault(s => s.FieldName == column.FieldName);
            
            if (sort != null)
            {
                column.SortDirection = sort.Direction;
                column.SortIndex = _dataSource.SortDescriptors.ToList().IndexOf(sort);
            }
            else
            {
                column.SortDirection = null;
                column.SortIndex = -1;
            }
        }
    }

    private void RefreshView()
    {
        // Bind data to rows presenter
        if (_rowsPresenter != null)
        {
            _rowsPresenter.ItemsSource = _dataSource.View;
        }

        // Get frozen and scrollable columns
        var frozenColumns = Columns.GetFrozenColumns().ToList();
        var scrollableColumns = Columns.GetScrollableColumns().ToList();
        var hasFrozenColumns = frozenColumns.Count > 0;

        // Bind frozen columns to frozen header presenter
        if (_frozenHeaderPresenter != null)
        {
            _frozenHeaderPresenter.ItemsSource = frozenColumns;
            _frozenHeaderPresenter.IsVisible = hasFrozenColumns;
        }

        // Show/hide frozen separator
        if (_frozenHeaderSeparator != null)
        {
            _frozenHeaderSeparator.IsVisible = hasFrozenColumns;
        }

        // Bind scrollable columns to header presenter
        if (_headerPresenter != null)
        {
            _headerPresenter.ItemsSource = scrollableColumns;
        }

        // Bind frozen columns to frozen filter presenter
        if (_frozenFilterPresenter != null)
        {
            _frozenFilterPresenter.ItemsSource = frozenColumns;
            _frozenFilterPresenter.IsVisible = hasFrozenColumns;
        }

        // Show/hide frozen filter separator
        if (_frozenFilterSeparator != null)
        {
            _frozenFilterSeparator.IsVisible = hasFrozenColumns;
        }

        // Bind scrollable columns to filter presenter
        if (_filterRowPresenter != null)
        {
            _filterRowPresenter.ItemsSource = scrollableColumns;
        }
    }

    #endregion

    #region Keyboard Navigation

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Up:
                MoveSelection(-1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;

            case Key.Down:
                MoveSelection(1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;

            case Key.PageUp:
                MoveSelection(-GetVisibleRowCount(), e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;

            case Key.PageDown:
                MoveSelection(GetVisibleRowCount(), e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;

            case Key.Home:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    MoveToFirst(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;

            case Key.End:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    MoveToLast(e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;

            case Key.A:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && SelectionMode == SelectionMode.Multiple)
                {
                    _selection.SelectAll(_dataSource.View);
                    e.Handled = true;
                }
                break;

            case Key.Enter:
                if (_editingItem != null)
                    CommitEdit();
                else if (_selection.CurrentItem != null)
                    RowDoubleClickCommand?.Execute(_selection.CurrentItem);
                e.Handled = true;
                break;

            case Key.Escape:
                CancelEdit();
                e.Handled = true;
                break;

            case Key.F2:
                if (_selection.CurrentItem != null && _selection.CurrentColumn != null)
                    BeginEdit(_selection.CurrentItem, _selection.CurrentColumn);
                e.Handled = true;
                break;
        }
    }

    private void MoveSelection(int delta, bool extend)
    {
        var currentIndex = _selection.CurrentRowIndex;
        var newIndex = Math.Clamp(currentIndex + delta, 0, _dataSource.FilteredCount - 1);

        if (newIndex == currentIndex) return;

        var item = _dataSource.GetItemAt(newIndex);
        if (item == null) return;

        if (extend && SelectionMode == SelectionMode.Multiple)
        {
            _selection.SelectRange(
                Enumerable.Range(Math.Min(currentIndex, newIndex), Math.Abs(delta) + 1)
                    .Select(i => _dataSource.GetItemAt(i)!)
                    .Where(i => i != null),
                Enumerable.Range(Math.Min(currentIndex, newIndex), Math.Abs(delta) + 1));
        }
        else
        {
            _selection.Select(item, newIndex);
        }

        ScrollIntoView(item);
    }

    private void MoveToFirst(bool extend)
    {
        var item = _dataSource.GetItemAt(0);
        if (item != null)
        {
            _selection.Select(item, 0);
            ScrollIntoView(item);
        }
    }

    private void MoveToLast(bool extend)
    {
        var lastIndex = _dataSource.FilteredCount - 1;
        var item = _dataSource.GetItemAt(lastIndex);
        if (item != null)
        {
            _selection.Select(item, lastIndex);
            ScrollIntoView(item);
        }
    }

    private int GetVisibleRowCount()
    {
        if (_scrollViewer == null) return 10;
        return (int)(_scrollViewer.Viewport.Height / RowHeight);
    }

    #endregion
}

#region Enums

public enum GridLinesVisibility
{
    None,
    Horizontal,
    Vertical,
    All
}

#endregion

#region Event Args

public class CellEditEventArgs : RoutedEventArgs
{
    public object Item { get; }
    public GridColumn Column { get; }
    public bool Cancel { get; set; }
    public object? NewValue { get; set; }

    public CellEditEventArgs(RoutedEvent routedEvent, object item, GridColumn column)
        : base(routedEvent)
    {
        Item = item;
        Column = column;
    }
}

public class ColumnEventArgs : RoutedEventArgs
{
    public GridColumn Column { get; }

    public ColumnEventArgs(RoutedEvent routedEvent, GridColumn column)
        : base(routedEvent)
    {
        Column = column;
    }
}

#endregion
