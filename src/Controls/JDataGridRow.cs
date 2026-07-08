using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Julien.Avalonia.DataGrid.Models;

namespace Julien.Avalonia.DataGrid.Controls;

/// <summary>
/// Represents a data row in the DataGrid.
/// </summary>
public class JDataGridRow : TemplatedControl
{
    #region Private Fields

    private ItemsControl? _cellsPresenter;
    private ItemsControl? _frozenCellsPresenter;
    private Border? _frozenSeparator;
    private Border? _rowIndicator;
    private TextBlock? _rowNumber;

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<int> RowIndexProperty =
        AvaloniaProperty.Register<JDataGridRow, int>(nameof(RowIndex), -1);

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<JDataGridRow, bool>(nameof(IsSelected), false);

    public static readonly StyledProperty<bool> IsCurrentProperty =
        AvaloniaProperty.Register<JDataGridRow, bool>(nameof(IsCurrent), false);

    public static readonly StyledProperty<bool> IsAlternateProperty =
        AvaloniaProperty.Register<JDataGridRow, bool>(nameof(IsAlternate), false);

    public static readonly StyledProperty<bool> IsEditingProperty =
        AvaloniaProperty.Register<JDataGridRow, bool>(nameof(IsEditing), false);

    public static readonly StyledProperty<double> RowHeightProperty =
        AvaloniaProperty.Register<JDataGridRow, double>(nameof(RowHeight), 36);

    public static readonly StyledProperty<bool> ShowRowNumberProperty =
        AvaloniaProperty.Register<JDataGridRow, bool>(nameof(ShowRowNumber), false);

    public static readonly StyledProperty<bool> ShowRowIndicatorProperty =
        AvaloniaProperty.Register<JDataGridRow, bool>(nameof(ShowRowIndicator), true);

    public static readonly StyledProperty<GridColumnCollection?> ColumnsProperty =
        AvaloniaProperty.Register<JDataGridRow, GridColumnCollection?>(nameof(Columns));

    public static readonly StyledProperty<IBrush?> SelectedBackgroundProperty =
        AvaloniaProperty.Register<JDataGridRow, IBrush?>(nameof(SelectedBackground));

    public static readonly StyledProperty<IBrush?> AlternateBackgroundProperty =
        AvaloniaProperty.Register<JDataGridRow, IBrush?>(nameof(AlternateBackground));

    public static readonly StyledProperty<IBrush?> HoverBackgroundProperty =
        AvaloniaProperty.Register<JDataGridRow, IBrush?>(nameof(HoverBackground));

    #endregion

    #region Routed Events

    public static readonly RoutedEvent<RowEventArgs> RowClickEvent =
        RoutedEvent.Register<JDataGridRow, RowEventArgs>(
            nameof(RowClick), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RowEventArgs> RowDoubleClickEvent =
        RoutedEvent.Register<JDataGridRow, RowEventArgs>(
            nameof(RowDoubleClick), RoutingStrategies.Bubble);

    public event EventHandler<RowEventArgs>? RowClick
    {
        add => AddHandler(RowClickEvent, value);
        remove => RemoveHandler(RowClickEvent, value);
    }

    public event EventHandler<RowEventArgs>? RowDoubleClick
    {
        add => AddHandler(RowDoubleClickEvent, value);
        remove => RemoveHandler(RowDoubleClickEvent, value);
    }

    #endregion

    #region Properties

    public int RowIndex
    {
        get => GetValue(RowIndexProperty);
        set => SetValue(RowIndexProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsCurrent
    {
        get => GetValue(IsCurrentProperty);
        set => SetValue(IsCurrentProperty, value);
    }

    public bool IsAlternate
    {
        get => GetValue(IsAlternateProperty);
        set => SetValue(IsAlternateProperty, value);
    }

    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public double RowHeight
    {
        get => GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    public bool ShowRowNumber
    {
        get => GetValue(ShowRowNumberProperty);
        set => SetValue(ShowRowNumberProperty, value);
    }

    public bool ShowRowIndicator
    {
        get => GetValue(ShowRowIndicatorProperty);
        set => SetValue(ShowRowIndicatorProperty, value);
    }

    public GridColumnCollection? Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public IBrush? SelectedBackground
    {
        get => GetValue(SelectedBackgroundProperty);
        set => SetValue(SelectedBackgroundProperty, value);
    }

    public IBrush? AlternateBackground
    {
        get => GetValue(AlternateBackgroundProperty);
        set => SetValue(AlternateBackgroundProperty, value);
    }

    public IBrush? HoverBackground
    {
        get => GetValue(HoverBackgroundProperty);
        set => SetValue(HoverBackgroundProperty, value);
    }

    #endregion

    #region Pseudo Classes

    private static readonly string PC_Selected = ":selected";
    private static readonly string PC_Current = ":current";
    private static readonly string PC_Alternate = ":alternate";
    private static readonly string PC_Editing = ":editing";
    private static readonly string PC_PointerOver = ":pointerover";

    static JDataGridRow()
    {
        IsSelectedProperty.Changed.AddClassHandler<JDataGridRow>((row, e) =>
            row.PseudoClasses.Set(PC_Selected, (bool)e.NewValue!));

        IsCurrentProperty.Changed.AddClassHandler<JDataGridRow>((row, e) =>
            row.PseudoClasses.Set(PC_Current, (bool)e.NewValue!));

        IsAlternateProperty.Changed.AddClassHandler<JDataGridRow>((row, e) =>
            row.PseudoClasses.Set(PC_Alternate, (bool)e.NewValue!));

        IsEditingProperty.Changed.AddClassHandler<JDataGridRow>((row, e) =>
            row.PseudoClasses.Set(PC_Editing, (bool)e.NewValue!));
    }

    #endregion

    #region Template

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _cellsPresenter = e.NameScope.Find<ItemsControl>("PART_CellsPresenter");
        _frozenCellsPresenter = e.NameScope.Find<ItemsControl>("PART_FrozenCellsPresenter");
        _frozenSeparator = e.NameScope.Find<Border>("PART_FrozenSeparator");
        _rowIndicator = e.NameScope.Find<Border>("PART_RowIndicator");
        _rowNumber = e.NameScope.Find<TextBlock>("PART_RowNumber");

        UpdateCells();
        UpdateRowNumber();
    }

    #endregion

    #region Pointer Events

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        PseudoClasses.Set(PC_PointerOver, true);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        PseudoClasses.Set(PC_PointerOver, false);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            RaiseEvent(new RowEventArgs(RowClickEvent, DataContext, RowIndex));

            if (e.ClickCount == 2)
            {
                RaiseEvent(new RowEventArgs(RowDoubleClickEvent, DataContext, RowIndex));
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Rebuilds the row's cells from the current column set. Called by the grid
    /// after a structural column change (reorder, freeze, visibility) so already
    /// realized rows reflect the new layout.
    /// </summary>
    public void RefreshCells() => UpdateCells();

    private void UpdateCells()
    {
        if (Columns == null)
            return;

        var frozenColumns = Columns.GetFrozenColumns().ToList();
        var scrollableColumns = Columns.GetScrollableColumns().ToList();
        var hasFrozenColumns = frozenColumns.Count > 0;

        if (_frozenCellsPresenter != null)
        {
            _frozenCellsPresenter.ItemsSource = frozenColumns;
            _frozenCellsPresenter.IsVisible = hasFrozenColumns;
        }

        if (_frozenSeparator != null)
        {
            _frozenSeparator.IsVisible = hasFrozenColumns;
        }

        if (_cellsPresenter != null)
        {
            _cellsPresenter.ItemsSource = scrollableColumns;
        }
    }

    private void UpdateRowNumber()
    {
        if (_rowNumber != null)
        {
            _rowNumber.Text = (RowIndex + 1).ToString();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == RowIndexProperty)
        {
            UpdateRowNumber();
        }
        else if (change.Property == ColumnsProperty)
        {
            // Rebuild cells only when the column set changes.
            UpdateCells();
        }
        // On DataContext change (row recycled to a new item during scroll) the
        // cells rebind RowData via their RelativeSource binding and refresh their
        // value; rebuilding the cell controls here would be redundant churn.
    }

    #endregion
}

#region Event Args

public class RowEventArgs : RoutedEventArgs
{
    public object? Item { get; }
    public int RowIndex { get; }

    public RowEventArgs(RoutedEvent routedEvent, object? item, int rowIndex)
        : base(routedEvent)
    {
        Item = item;
        RowIndex = rowIndex;
    }
}

#endregion
