using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.VisualTree;
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
    private ScrollViewer? _summaryScrollViewer;
    private ItemsControl? _summaryPresenter;
    private ItemsControl? _frozenSummaryPresenter;
    private Border? _frozenSummarySeparator;
    private JDataGridGroupPanel? _groupPanel;
    private Border? _frozenHeaderSeparator;
    private Border? _frozenFilterSeparator;
    private object? _editingItem;
    private GridColumn? _editingColumn;
    private JDataGridCell? _editingCell;
    private IDisposable? _widthSubscription;

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

    public static readonly StyledProperty<bool> ShowSummaryFooterProperty =
        AvaloniaProperty.Register<JDataGrid, bool>(nameof(ShowSummaryFooter), false);

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

    public bool ShowSummaryFooter
    {
        get => GetValue(ShowSummaryFooterProperty);
        set => SetValue(ShowSummaryFooterProperty, value);
    }

    /// <summary>
    /// Aggregates shown in the summary footer, one or more per column (matched by
    /// <see cref="GridSummary.FieldName"/>).
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<GridSummary> TotalSummaries { get; } = new();

    /// <summary>
    /// Aggregates shown in each group header when grouping is active.
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<GridSummary> GroupSummaries { get; } = new();

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
        TotalSummaries.CollectionChanged += (_, _) => UpdateSummaryFooter();
        GroupSummaries.CollectionChanged += (_, _) => { ComputeGroupSummaries(); _dataSource.RebuildVisualRows(); };
    }

    static JDataGrid()
    {
        ItemsSourceProperty.Changed.AddClassHandler<JDataGrid>((grid, e) => grid.OnItemsSourceChanged(e));
        SelectionModeProperty.Changed.AddClassHandler<JDataGrid>((grid, e) => grid.OnSelectionModeChanged(e));
        SelectedItemProperty.Changed.AddClassHandler<JDataGrid>((grid, e) => grid.OnSelectedItemChanged(e));
        ShowSummaryFooterProperty.Changed.AddClassHandler<JDataGrid>((grid, _) => grid.UpdateSummaryFooter());
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
        _summaryScrollViewer = e.NameScope.Find<ScrollViewer>("PART_SummaryScrollViewer");
        _summaryPresenter = e.NameScope.Find<ItemsControl>("PART_SummaryPresenter");
        _frozenSummaryPresenter = e.NameScope.Find<ItemsControl>("PART_FrozenSummaryPresenter");
        _frozenSummarySeparator = e.NameScope.Find<Border>("PART_FrozenSummarySeparator");
        _groupPanel = e.NameScope.Find<JDataGridGroupPanel>("PART_GroupPanel");
        _frozenHeaderSeparator = e.NameScope.Find<Border>("PART_FrozenHeaderSeparator");
        _frozenFilterSeparator = e.NameScope.Find<Border>("PART_FrozenFilterSeparator");

        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;

            // Recompute pixel widths (star/auto distribution) whenever the data
            // viewport is resized.
            _widthSubscription?.Dispose();
            _widthSubscription = _scrollViewer.GetObservable(BoundsProperty)
                .Subscribe(new AnonymousObserver<global::Avalonia.Rect>(_ =>
                {
                    RecalculateColumnWidths();
                    // Footer cells are a snapshot of widths; rebuild them when the
                    // viewport (and thus ActualWidth) changes so they stay aligned.
                    UpdateSummaryFooter();
                }));
        }

        // Listen to column header events (bubbled from JDataGridColumnHeader)
        AddHandler(JDataGridColumnHeader.SortRequestedEvent, OnColumnSortRequested);
        AddHandler(JDataGridColumnHeader.ResizeCompletedEvent, OnColumnResizeCompleted);
        AddHandler(JDataGridColumnHeader.ReorderCompletedEvent, OnColumnReorderCompleted);
        AddHandler(JDataGridColumnHeader.FreezeRequestedEvent, OnColumnFreezeRequested);
        AddHandler(JDataGridColumnHeader.AutoFitRequestedEvent, OnColumnAutoFitRequested);

        // Listen to filter cell events (bubbled from JDataGridFilterCell)
        AddHandler(JDataGridFilterCell.FilterChangedEvent, OnFilterChanged);

        // Listen to cell edit events (bubbled from JDataGridCell)
        AddHandler(JDataGridCell.BeginEditEvent, OnCellBeginEditRequested);
        AddHandler(JDataGridCell.EditEndedEvent, OnCellEditEnded);

        // Mouse selection (bubbled from rows/cells)
        AddHandler(JDataGridRow.RowClickEvent, OnRowClicked);
        AddHandler(JDataGridCell.CellClickEvent, OnCellClicked);

        // Listen to group panel events
        AddHandler(JDataGridGroupPanel.ColumnGroupedEvent, OnColumnGrouped);
        AddHandler(JDataGridGroupPanel.ColumnUngroupedEvent, OnColumnUngrouped);

        // Rebuild the visual rows when a group header is expanded/collapsed.
        AddHandler(JDataGridGroupRow.ExpandChangedEvent, OnGroupExpandChanged);

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
        var cell = FindCell(item, column);
        if (cell != null)
        {
            BeginEdit(cell);
        }
    }

    private void BeginEdit(JDataGridCell cell)
    {
        if (!AllowEditing || cell.Column == null || cell.RowData == null || cell.IsReadOnly)
            return;

        var args = new CellEditEventArgs(CellEditStartingEvent, cell.RowData, cell.Column);
        RaiseEvent(args);
        if (args.Cancel) return;

        // Commit any other cell currently being edited.
        if (_editingCell != null && _editingCell != cell)
        {
            _editingCell.CommitEditing();
        }

        _editingItem = cell.RowData;
        _editingColumn = cell.Column;
        _editingCell = cell;
        cell.IsEditing = true;
    }

    /// <summary>
    /// Commits the current edit.
    /// </summary>
    public void CommitEdit()
    {
        _editingCell?.CommitEditing();
    }

    /// <summary>
    /// Cancels the current edit, restoring the original value.
    /// </summary>
    public void CancelEdit()
    {
        _editingCell?.CancelEditing();
    }

    private void OnCellBeginEditRequested(object? sender, CellEventArgs e)
    {
        if (e.Source is JDataGridCell cell)
        {
            BeginEdit(cell);
            e.Handled = true;
        }
    }

    private void OnCellEditEnded(object? sender, CellEventArgs e)
    {
        if (_editingItem != null && _editingColumn != null)
        {
            var args = new CellEditEventArgs(CellEditEndingEvent, _editingItem, _editingColumn);
            RaiseEvent(args);
            CellEditEndingCommand?.Execute(args);
        }

        _editingItem = null;
        _editingColumn = null;
        _editingCell = null;
        e.Handled = true;
    }

    private JDataGridCell? FindCell(object item, GridColumn column)
    {
        if (_rowsPresenter == null) return null;

        return _rowsPresenter.GetVisualDescendants()
            .OfType<JDataGridCell>()
            .FirstOrDefault(c => Equals(c.RowData, item) && c.Column == column);
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
        _dataSource.RebuildVisualRows();
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
        _dataSource.RebuildVisualRows();
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

        UpdateRowSelectionStates();

        RaiseEvent(new global::Avalonia.Controls.SelectionChangedEventArgs(SelectionChangedEvent, (System.Collections.IList)e.RemovedItems, (System.Collections.IList)e.AddedItems));
        SelectionChangedCommand?.Execute(_selection.SelectedItem);
    }

    private void OnRowClicked(object? sender, RowEventArgs e)
    {
        if (e.Item == null) return;
        _selection.Select(e.Item, _dataSource.IndexOf(e.Item));
    }

    private void OnCellClicked(object? sender, CellEventArgs e)
    {
        if (e.Item == null) return;
        _selection.CurrentColumn = e.Column;

        // Mode cellule : la cellule cliquée devient la sélection (pas la ligne)
        if (SelectionMode == SelectionMode.Cell && e.Column != null)
        {
            SelectCellAt(_dataSource.IndexOf(e.Item), e.Column);
        }
        else
        {
            _selection.Select(e.Item, _dataSource.IndexOf(e.Item));
        }
    }

    /// <summary>
    /// Sélectionne une cellule (mode Cell) : état visuel + item courant + scroll
    /// </summary>
    private void SelectCellAt(int rowIndex, Models.GridColumn column)
    {
        if (rowIndex < 0 || rowIndex >= _dataSource.FilteredCount) return;

        _selection.SelectCell(rowIndex, column);
        var item = _dataSource.GetItemAt(rowIndex);
        _selection.CurrentItem = item;
        UpdateCellSelectionStates();
        if (item != null)
            ScrollIntoView(item);
    }

    /// <summary>
    /// Pousse la sélection de cellules vers les cellules réalisées (mode Cell)
    /// </summary>
    private void UpdateCellSelectionStates()
    {
        if (_rowsPresenter == null) return;

        foreach (var row in _rowsPresenter.GetVisualDescendants().OfType<JDataGridRow>())
        {
            var item = row.DataContext;
            var rowIndex = item != null ? _dataSource.IndexOf(item) : -1;

            // En mode cellule, la ligne n'est pas surlignée — seule la cellule l'est
            row.IsSelected = false;
            row.IsCurrent = item != null && ReferenceEquals(item, _selection.CurrentItem);

            foreach (var cell in row.GetVisualDescendants().OfType<JDataGridCell>())
            {
                cell.IsSelected = rowIndex >= 0 && cell.Column != null &&
                    _selection.IsCellSelected(rowIndex, cell.Column);
            }
        }
    }

    private void MoveCellHorizontal(int delta, bool wrap)
    {
        var columns = Columns.GetVisibleColumns().ToList();
        if (columns.Count == 0) return;

        var currentIndex = _selection.CurrentColumn != null
            ? columns.IndexOf(_selection.CurrentColumn)
            : 0;
        if (currentIndex < 0) currentIndex = 0;

        var rowIndex = Math.Max(0, _selection.CurrentRowIndex);
        var newIndex = currentIndex + delta;

        if (wrap)
        {
            // Tab en fin de ligne -> première cellule de la ligne suivante (et inverse)
            if (newIndex >= columns.Count)
            {
                if (rowIndex >= _dataSource.FilteredCount - 1) return;
                newIndex = 0;
                rowIndex++;
            }
            else if (newIndex < 0)
            {
                if (rowIndex <= 0) return;
                newIndex = columns.Count - 1;
                rowIndex--;
            }
        }
        else
        {
            newIndex = Math.Clamp(newIndex, 0, columns.Count - 1);
        }

        SelectCellAt(rowIndex, columns[newIndex]);
    }

    private void MoveCellVertical(int delta)
    {
        var column = _selection.CurrentColumn ?? Columns.GetVisibleColumns().FirstOrDefault();
        if (column == null) return;

        var rowIndex = Math.Clamp(_selection.CurrentRowIndex + delta,
            0, Math.Max(0, _dataSource.FilteredCount - 1));
        SelectCellAt(rowIndex, column);
    }

    /// <summary>
    /// Pushes the current selection to the realized data rows so they highlight.
    /// </summary>
    private void UpdateRowSelectionStates()
    {
        if (_rowsPresenter == null) return;

        foreach (var row in _rowsPresenter.GetVisualDescendants().OfType<JDataGridRow>())
        {
            var item = row.DataContext;
            row.IsSelected = item != null && _selection.IsSelected(item);
            row.IsCurrent = item != null && ReferenceEquals(item, _selection.CurrentItem);
        }
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

            if (_summaryScrollViewer != null)
            {
                _summaryScrollViewer.Offset = new global::Avalonia.Vector(horizontalOffset, _summaryScrollViewer.Offset.Y);
            }
        }
    }

    private void OnColumnSortRequested(object? sender, ColumnEventArgs e)
    {
        if (!AllowSorting || e.Column == null) return;

        // Shift+clic : tri multi-colonnes (le tri existant est conservé, la
        // colonne cliquée s'ajoute comme critère suivant)
        var append = e.Modifiers.HasFlag(KeyModifiers.Shift);
        _dataSource.ToggleSort(e.Column.FieldName, append);
        UpdateColumnSortIndicators();

        e.Handled = true;
    }

    private void OnColumnResizeCompleted(object? sender, ColumnResizeEventArgs e)
    {
        // Redistribute remaining width among star columns after a fixed resize.
        RecalculateColumnWidths();
        e.Handled = true;
    }

    /// <summary>
    /// Computes each visible column's pixel <see cref="GridColumn.ActualWidth"/>
    /// from its Width mode (fixed/auto/star) and the available viewport, so star
    /// and auto columns render at real widths instead of their raw GridLength value.
    /// </summary>
    private void RecalculateColumnWidths()
    {
        double total = _scrollViewer?.Bounds.Width ?? 0;
        if (total <= 0) total = Bounds.Width;
        if (total <= 0 || Columns.Count == 0) return;

        // Size frozen columns first; they occupy the left chrome alongside the row
        // indicator and row-number gutter.
        var frozen = Columns.GetFrozenColumns().ToList();
        if (frozen.Count > 0)
        {
            double frozenWidth = frozen.Sum(c => c.Width.IsAbsolute ? c.Width.Value : Math.Max(c.MinWidth, 80));
            Helpers.ColumnWidthHelper.CalculateColumnWidths(frozen, frozenWidth);
        }

        // Scrollable columns share the width remaining after the left chrome
        // (indicator + row number + frozen cells + frozen separator), so star
        // columns don't overflow the viewport and clip the last column.
        double chrome = 0;
        if (ShowRowIndicator) chrome += 4;
        if (ShowRowNumbers) chrome += 40;
        chrome += frozen.Sum(c => c.ActualWidth);
        if (frozen.Count > 0) chrome += 2;

        var scrollable = Columns.GetScrollableColumns().ToList();
        if (scrollable.Count > 0)
            Helpers.ColumnWidthHelper.CalculateColumnWidths(scrollable, Math.Max(0, total - chrome));
    }

    private void OnColumnReorderCompleted(object? sender, ColumnReorderEventArgs e)
    {
        if (!AllowColumnReordering) return;

        Columns.MoveColumn(e.Column, e.NewIndex);
        RefreshView();
        e.Handled = true;
    }

    private void OnColumnAutoFitRequested(object? sender, ColumnEventArgs e)
    {
        if (e.Column == null) return;
        AutoFitColumn(e.Column);
        e.Handled = true;
    }

    /// <summary>
    /// Ajuste la largeur de la colonne à son contenu (en-tête + 100 premières
    /// lignes de la vue courante, formatées comme les cellules)
    /// </summary>
    public void AutoFitColumn(Models.GridColumn column)
    {
        double MeasureText(string text, global::Avalonia.Media.FontWeight weight)
            => new global::Avalonia.Media.FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                global::Avalonia.Media.FlowDirection.LeftToRight,
                new global::Avalonia.Media.Typeface(FontFamily, weight: weight),
                FontSize > 0 ? FontSize : 14,
                global::Avalonia.Media.Brushes.Black).Width;

        // En-tête : padding + glyphes tri/pin
        var headerWidth = MeasureText(column.Header ?? "", global::Avalonia.Media.FontWeight.SemiBold) + 52;

        var width = Helpers.ColumnWidthHelper.MeasureColumnWidth(
            column,
            _dataSource.View,
            (item, col) => MeasureText(col.GetDisplayText(item), global::Avalonia.Media.FontWeight.Normal) + 18,
            headerWidth);

        width = Math.Clamp(Math.Ceiling(width), column.MinWidth,
            double.IsInfinity(column.MaxWidth) ? double.MaxValue : column.MaxWidth);

        column.Width = new GridLength(width);
        column.ActualWidth = width;
        RecalculateColumnWidths();
        RefreshView();
    }

    // ═══════════════════════════════════════════════════════════════
    // COPIE PRESSE-PAPIER + EXPORT CSV
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Copie les lignes sélectionnées dans le presse-papier (TSV, en-têtes
    /// incluses), dans l'ordre de la vue courante. Sans sélection : rien.
    /// </summary>
    public async Task CopySelectionToClipboardAsync()
    {
        var selected = new HashSet<object>(_selection.SelectedItems);
        if (selected.Count == 0 && _selection.CurrentItem != null)
            selected.Add(_selection.CurrentItem);
        if (selected.Count == 0) return;

        var rows = _dataSource.View.Where(selected.Contains);
        var text = BuildDelimitedText(rows, '\t', includeHeaders: true, quote: false);

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(text);
    }

    /// <summary>
    /// Exporte la vue courante (tri + filtres appliqués) en CSV.
    /// Séparateur ';' par défaut (locales à virgule décimale).
    /// </summary>
    public string ToCsv(char separator = ';', bool includeHeaders = true)
        => BuildDelimitedText(_dataSource.View, separator, includeHeaders, quote: true);

    /// <summary>Exporte la vue courante en fichier CSV (UTF-8 avec BOM, pour Excel)</summary>
    public async Task ExportCsvAsync(string path, char separator = ';', bool includeHeaders = true)
    {
        var csv = ToCsv(separator, includeHeaders);
        await System.IO.File.WriteAllTextAsync(path, csv,
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private string BuildDelimitedText(IEnumerable<object> rows, char separator,
        bool includeHeaders, bool quote)
    {
        var columns = Columns.GetVisibleColumns().ToList();
        var sb = new System.Text.StringBuilder();

        string Escape(string field)
        {
            if (!quote) return field.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
            if (field.Contains(separator) || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return $"\"{field.Replace("\"", "\"\"")}\"";
            return field;
        }

        if (includeHeaders)
            sb.AppendLine(string.Join(separator, columns.Select(c => Escape(c.Header ?? c.FieldName))));

        foreach (var row in rows)
            sb.AppendLine(string.Join(separator, columns.Select(c => Escape(c.GetDisplayText(row)))));

        return sb.ToString();
    }

    private void OnColumnFreezeRequested(object? sender, ColumnFreezeEventArgs e)
    {
        if (e.Column == null) return;

        // Le pin GÈLE réellement la colonne (presenter figé à gauche) — il ne
        // faisait que verrouiller la position. Une colonne gelée est aussi
        // verrouillée (non réordonnable).
        e.Column.IsFrozen = e.Freeze;
        e.Column.IsPositionLocked = e.Freeze;
        RecalculateColumnWidths();
        RefreshView();
        e.Handled = true;
    }

    private void OnFilterChanged(object? sender, FilterChangedEventArgs e)
    {
        if (!AllowFiltering || e.Column == null) return;

        var valueless = JDataGridFilterCell.IsValueless(e.Operator);

        if (string.IsNullOrWhiteSpace(e.FilterText) && !valueless)
        {
            // Remove filter for this column (les opérateurs sans valeur
            // — Vide, Aujourd'hui... — s'appliquent avec un texte vide)
            _dataSource.RemoveFilter(e.Column.FieldName);
        }
        else
        {
            // Add or update filter for this column, with the selected operator
            var filter = new Models.GridFilter
            {
                FieldName = e.Column.FieldName,
                Operator = e.Operator,
                Value = valueless ? null : e.FilterText,
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

    private void OnGroupExpandChanged(object? sender, GroupEventArgs e)
    {
        // The group's IsExpanded was already updated by the group row; rebuild
        // the flattened visual rows so collapsed items disappear / reappear.
        _dataSource.RebuildVisualRows();
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
        // Ensure columns have real pixel widths before cells/footer use them.
        RecalculateColumnWidths();

        // Compute group header aggregates before binding so realized group rows
        // pick up the text.
        ComputeGroupSummaries();

        // Bind data to rows presenter (interleaved group headers + data rows).
        if (_rowsPresenter != null)
        {
            _rowsPresenter.ItemsSource = _dataSource.VisualRows;

            // Rows no longer rebuild their cells on recycle (cells rebind instead),
            // so push column-structure changes (reorder/freeze/visibility) to any
            // already realized rows here.
            foreach (var row in _rowsPresenter.GetVisualDescendants().OfType<JDataGridRow>())
            {
                row.RefreshCells();
            }
        }

        // Get frozen and scrollable columns
        var frozenColumns = Columns.GetFrozenColumns().ToList();
        var scrollableColumns = Columns.GetScrollableColumns().ToList();
        var hasFrozenColumns = frozenColumns.Count > 0;

        // Bind columns to the header/filter presenters only when the column set
        // actually changed. Reassigning on every data refresh would recreate the
        // header and filter cells, clearing in-progress filter text and stealing
        // focus on each keystroke.
        SetColumnsIfChanged(_frozenHeaderPresenter, frozenColumns);
        if (_frozenHeaderPresenter != null) _frozenHeaderPresenter.IsVisible = hasFrozenColumns;
        if (_frozenHeaderSeparator != null) _frozenHeaderSeparator.IsVisible = hasFrozenColumns;

        SetColumnsIfChanged(_headerPresenter, scrollableColumns);

        SetColumnsIfChanged(_frozenFilterPresenter, frozenColumns);
        if (_frozenFilterPresenter != null) _frozenFilterPresenter.IsVisible = hasFrozenColumns;
        if (_frozenFilterSeparator != null) _frozenFilterSeparator.IsVisible = hasFrozenColumns;

        SetColumnsIfChanged(_filterRowPresenter, scrollableColumns);

        UpdateSummaryFooter();
    }

    private static void SetColumnsIfChanged(ItemsControl? presenter, List<GridColumn> columns)
    {
        if (presenter == null) return;
        if (presenter.ItemsSource is IEnumerable<GridColumn> current && current.SequenceEqual(columns))
            return;
        presenter.ItemsSource = columns;
    }

    /// <summary>
    /// Recomputes the summary footer cells (one per visible column, aligned with
    /// the rows) over the current filtered view and binds them to the presenters.
    /// </summary>
    private void UpdateSummaryFooter()
    {
        if (_summaryPresenter == null && _frozenSummaryPresenter == null)
            return;

        var view = _dataSource.View;

        if (_frozenSummaryPresenter != null)
        {
            var frozen = Columns.GetFrozenColumns().ToList();
            _frozenSummaryPresenter.ItemsSource = BuildFooterCells(frozen, view);
            _frozenSummaryPresenter.IsVisible = frozen.Count > 0;
        }

        if (_frozenSummarySeparator != null)
        {
            _frozenSummarySeparator.IsVisible = Columns.GetFrozenColumns().Any();
        }

        if (_summaryPresenter != null)
        {
            _summaryPresenter.ItemsSource = BuildFooterCells(Columns.GetScrollableColumns().ToList(), view);
        }
    }

    /// <summary>
    /// Computes the aggregate text for every group header from <see cref="GroupSummaries"/>.
    /// </summary>
    private void ComputeGroupSummaries()
    {
        if (!_dataSource.IsGrouped) return;

        foreach (var group in _dataSource.Groups)
        {
            ComputeGroupSummary(group);
        }
    }

    private void ComputeGroupSummary(GridGroup group)
    {
        if (GroupSummaries.Count > 0)
        {
            var items = group.GetAllItems().ToList();
            group.SummaryText = string.Join("  |  ",
                GroupSummaries.Select(s => s.ComputeText(items)).Where(t => !string.IsNullOrEmpty(t)));
        }

        foreach (var subGroup in group.SubGroups)
        {
            ComputeGroupSummary(subGroup);
        }
    }

    private List<GridFooterCell> BuildFooterCells(IReadOnlyList<GridColumn> columns, IReadOnlyList<object> view)
    {
        var cells = new List<GridFooterCell>(columns.Count);
        foreach (var column in columns)
        {
            var summary = TotalSummaries.FirstOrDefault(s => s.FieldName == column.FieldName);
            cells.Add(new GridFooterCell
            {
                Width = column.ActualWidth,
                Text = summary?.ComputeText(view) ?? string.Empty,
                TextAlignment = column.TextAlignment
            });
        }
        return cells;
    }

    #endregion

    #region Keyboard Navigation

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Up:
                if (SelectionMode == SelectionMode.Cell)
                    MoveCellVertical(-1);
                else
                    MoveSelection(-1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;

            case Key.Down:
                if (SelectionMode == SelectionMode.Cell)
                    MoveCellVertical(1);
                else
                    MoveSelection(1, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
                e.Handled = true;
                break;

            case Key.Left:
                if (SelectionMode == SelectionMode.Cell)
                {
                    MoveCellHorizontal(-1, wrap: false);
                    e.Handled = true;
                }
                break;

            case Key.Right:
                if (SelectionMode == SelectionMode.Cell)
                {
                    MoveCellHorizontal(1, wrap: false);
                    e.Handled = true;
                }
                break;

            case Key.Tab:
                if (SelectionMode == SelectionMode.Cell)
                {
                    MoveCellHorizontal(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1, wrap: true);
                    e.Handled = true;
                }
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

            case Key.C:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    _ = CopySelectionToClipboardAsync();
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
                if (_selection.CurrentItem != null)
                {
                    var column = _selection.CurrentColumn
                        ?? Columns.FirstOrDefault(c => c.IsVisible && !c.IsReadOnly);
                    if (column != null)
                        BeginEdit(_selection.CurrentItem, column);
                }
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

    /// <summary>Modificateurs clavier au moment du geste (Shift+clic = multi-tri)</summary>
    public KeyModifiers Modifiers { get; init; }

    public ColumnEventArgs(RoutedEvent routedEvent, GridColumn column)
        : base(routedEvent)
    {
        Column = column;
    }
}

#endregion
