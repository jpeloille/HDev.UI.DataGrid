using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using HDev.UI.DataGrid.Models;
using System.ComponentModel;
using System.Threading.Tasks;

namespace HDev.UI.DataGrid;

/// <summary>
/// Represents a column header with sorting, resizing, and reordering support.
/// </summary>
public class HDevDataGridColumnHeader : TemplatedControl
{
    #region Private Fields

    private Border? _resizeGrip;
    private bool _isResizing;
    private Point _resizeStartPoint;
    private double _originalWidth;
    private bool _isDragging;
    private bool _isPointerPressed;
    private Point _dragStartPoint;
    // DoDragDropAsync exige l'appui d'origine ; le seuil n'est franchi qu'au Moved.
    private PointerPressedEventArgs? _dragPressArgs;
    private const double DragThreshold = 5.0;

    /// <summary>
    /// Format de glisser-déposer d'une colonne (en-tête → réordonnancement ou zone de groupement).
    /// Reste dans le processus : jamais sérialisé vers la plateforme.
    /// </summary>
    internal static readonly DataFormat<GridColumn> ColumnDragFormat =
        DataFormat.CreateInProcessFormat<GridColumn>("GridColumn");
    private Border? _dropIndicatorLeft;
    private Border? _dropIndicatorRight;

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<GridColumn?> ColumnProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, GridColumn?>(nameof(Column));

    public static readonly StyledProperty<string> HeaderTextProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, string>(nameof(HeaderText), string.Empty);

    public static readonly StyledProperty<ListSortDirection?> SortDirectionProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, ListSortDirection?>(nameof(SortDirection));

    public static readonly StyledProperty<int> SortIndexProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, int>(nameof(SortIndex), -1);

    public static readonly StyledProperty<bool> AllowSortProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(AllowSort), true);

    public static readonly StyledProperty<bool> AllowResizeProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(AllowResize), true);

    public static readonly StyledProperty<bool> AllowReorderProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(AllowReorder), true);

    public static readonly StyledProperty<bool> ShowFilterButtonProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(ShowFilterButton), false);

    public static readonly StyledProperty<bool> IsFilteredProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(IsFiltered), false);

    public static readonly StyledProperty<bool> IsDragOverLeftProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(IsDragOverLeft), false);

    public static readonly StyledProperty<bool> IsDragOverRightProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(IsDragOverRight), false);

    public static readonly StyledProperty<bool> IsFrozenProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(IsFrozen), false);

    public static readonly StyledProperty<bool> IsPositionLockedProperty =
        AvaloniaProperty.Register<HDevDataGridColumnHeader, bool>(nameof(IsPositionLocked), false);

    #endregion

    #region Routed Events

    public static readonly RoutedEvent<ColumnEventArgs> SortRequestedEvent =
        RoutedEvent.Register<HDevDataGridColumnHeader, ColumnEventArgs>(
            nameof(SortRequested), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ColumnResizeEventArgs> ResizeCompletedEvent =
        RoutedEvent.Register<HDevDataGridColumnHeader, ColumnResizeEventArgs>(
            nameof(ResizeCompleted), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ColumnReorderEventArgs> ReorderCompletedEvent =
        RoutedEvent.Register<HDevDataGridColumnHeader, ColumnReorderEventArgs>(
            nameof(ReorderCompleted), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ColumnEventArgs> FilterRequestedEvent =
        RoutedEvent.Register<HDevDataGridColumnHeader, ColumnEventArgs>(
            nameof(FilterRequested), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ColumnEventArgs> GroupRequestedEvent =
        RoutedEvent.Register<HDevDataGridColumnHeader, ColumnEventArgs>(
            nameof(GroupRequested), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ColumnFreezeEventArgs> FreezeRequestedEvent =
        RoutedEvent.Register<HDevDataGridColumnHeader, ColumnFreezeEventArgs>(
            nameof(FreezeRequested), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<ColumnEventArgs> AutoFitRequestedEvent =
        RoutedEvent.Register<HDevDataGridColumnHeader, ColumnEventArgs>(
            nameof(AutoFitRequested), RoutingStrategies.Bubble);

    public event EventHandler<ColumnEventArgs>? AutoFitRequested
    {
        add => AddHandler(AutoFitRequestedEvent, value);
        remove => RemoveHandler(AutoFitRequestedEvent, value);
    }

    public event EventHandler<ColumnEventArgs>? SortRequested
    {
        add => AddHandler(SortRequestedEvent, value);
        remove => RemoveHandler(SortRequestedEvent, value);
    }

    public event EventHandler<ColumnResizeEventArgs>? ResizeCompleted
    {
        add => AddHandler(ResizeCompletedEvent, value);
        remove => RemoveHandler(ResizeCompletedEvent, value);
    }

    public event EventHandler<ColumnReorderEventArgs>? ReorderCompleted
    {
        add => AddHandler(ReorderCompletedEvent, value);
        remove => RemoveHandler(ReorderCompletedEvent, value);
    }

    public event EventHandler<ColumnEventArgs>? FilterRequested
    {
        add => AddHandler(FilterRequestedEvent, value);
        remove => RemoveHandler(FilterRequestedEvent, value);
    }

    public event EventHandler<ColumnEventArgs>? GroupRequested
    {
        add => AddHandler(GroupRequestedEvent, value);
        remove => RemoveHandler(GroupRequestedEvent, value);
    }

    public event EventHandler<ColumnFreezeEventArgs>? FreezeRequested
    {
        add => AddHandler(FreezeRequestedEvent, value);
        remove => RemoveHandler(FreezeRequestedEvent, value);
    }

    #endregion

    #region Properties

    public GridColumn? Column
    {
        get => GetValue(ColumnProperty);
        set => SetValue(ColumnProperty, value);
    }

    public string HeaderText
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public ListSortDirection? SortDirection
    {
        get => GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }

    public int SortIndex
    {
        get => GetValue(SortIndexProperty);
        set => SetValue(SortIndexProperty, value);
    }

    public bool AllowSort
    {
        get => GetValue(AllowSortProperty);
        set => SetValue(AllowSortProperty, value);
    }

    public bool AllowResize
    {
        get => GetValue(AllowResizeProperty);
        set => SetValue(AllowResizeProperty, value);
    }

    public bool AllowReorder
    {
        get => GetValue(AllowReorderProperty);
        set => SetValue(AllowReorderProperty, value);
    }

    public bool ShowFilterButton
    {
        get => GetValue(ShowFilterButtonProperty);
        set => SetValue(ShowFilterButtonProperty, value);
    }

    public bool IsFiltered
    {
        get => GetValue(IsFilteredProperty);
        set => SetValue(IsFilteredProperty, value);
    }

    public bool IsDragOverLeft
    {
        get => GetValue(IsDragOverLeftProperty);
        set => SetValue(IsDragOverLeftProperty, value);
    }

    public bool IsDragOverRight
    {
        get => GetValue(IsDragOverRightProperty);
        set => SetValue(IsDragOverRightProperty, value);
    }

    public bool IsFrozen
    {
        get => GetValue(IsFrozenProperty);
        set => SetValue(IsFrozenProperty, value);
    }

    public bool IsPositionLocked
    {
        get => GetValue(IsPositionLockedProperty);
        set => SetValue(IsPositionLockedProperty, value);
    }

    #endregion

    #region Constructor

    static HDevDataGridColumnHeader()
    {
        ColumnProperty.Changed.AddClassHandler<HDevDataGridColumnHeader>((header, e) => header.OnColumnChanged(e));
    }

    #endregion

    #region Template

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _resizeGrip = e.NameScope.Find<Border>("PART_ResizeGrip");
        _dropIndicatorLeft = e.NameScope.Find<Border>("PART_DropIndicatorLeft");
        _dropIndicatorRight = e.NameScope.Find<Border>("PART_DropIndicatorRight");

        if (_resizeGrip != null)
        {
            _resizeGrip.PointerPressed += OnResizeGripPointerPressed;
            _resizeGrip.PointerMoved += OnResizeGripPointerMoved;
            _resizeGrip.PointerReleased += OnResizeGripPointerReleased;
        }

        var filterButton = e.NameScope.Find<Button>("PART_FilterButton");
        if (filterButton != null)
        {
            filterButton.Click += OnFilterButtonClick;
        }

        var pinButton = e.NameScope.Find<Button>("PART_PinButton");
        if (pinButton != null)
        {
            pinButton.Click += OnPinButtonClick;
        }

        // Enable drag-drop for column reordering
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    #endregion

    #region Event Handlers

    private void OnColumnChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is GridColumn column)
        {
            HeaderText = column.Header;
            SortDirection = column.SortDirection;
            SortIndex = column.SortIndex;
            AllowSort = column.AllowSort;
            AllowResize = column.AllowResize;
            AllowReorder = column.AllowReorder;
            IsFrozen = column.IsFrozen;
            IsPositionLocked = column.IsPositionLocked;

            column.PropertyChanged += OnColumnPropertyChanged;
        }

        if (e.OldValue is GridColumn oldColumn)
        {
            oldColumn.PropertyChanged -= OnColumnPropertyChanged;
        }
    }

    private void OnColumnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not GridColumn column) return;

        if (e.Property == GridColumn.SortDirectionProperty)
        {
            SortDirection = column.SortDirection;
        }
        else if (e.Property == GridColumn.SortIndexProperty)
        {
            SortIndex = column.SortIndex;
        }
        else if (e.Property == GridColumn.IsFrozenProperty)
        {
            IsFrozen = column.IsFrozen;
        }
        else if (e.Property == GridColumn.IsPositionLockedProperty)
        {
            IsPositionLocked = column.IsPositionLocked;
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.Source == _resizeGrip) return;

        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsLeftButtonPressed)
        {
            _dragStartPoint = point.Position;
            _dragPressArgs = e;
            _isPointerPressed = true;

            if (e.ClickCount == 2 && AllowResize)
            {
                AutoFitWidth();
                e.Handled = true;
                _isPointerPressed = false;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        var wasDragging = _isDragging;
        _isDragging = false;
        _isPointerPressed = false;
        _dragPressArgs = null;

        if (wasDragging)
        {
            return;
        }

        if (!_isResizing && AllowSort && Column != null)
        {
            RaiseEvent(new ColumnEventArgs(SortRequestedEvent, Column)
            {
                Modifiers = e.KeyModifiers
            });
        }
    }

    protected override async void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // Don't allow dragging if column is locked or reordering is disabled
        if (!_isPointerPressed || _isDragging || _isResizing || Column == null || IsPositionLocked || !AllowReorder
            || _dragPressArgs == null)
            return;

        var point = e.GetCurrentPoint(this);
        var distance = Math.Sqrt(
            Math.Pow(point.Position.X - _dragStartPoint.X, 2) +
            Math.Pow(point.Position.Y - _dragStartPoint.Y, 2));

        if (distance >= DragThreshold)
        {
            _isDragging = true;
            _isPointerPressed = false;

            // Start drag operation
            var pressArgs = _dragPressArgs;
            _dragPressArgs = null;
            var data = new DataTransfer();
            data.Add(DataTransferItem.Create(ColumnDragFormat, Column));

            await DragDrop.DoDragDropAsync(pressArgs, data, DragDropEffects.Move);
            _isDragging = false;
        }
    }

    #region Drag/Drop for Reordering

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        // Don't allow dropping on locked columns
        if (!AllowReorder || Column == null || IsPositionLocked) return;

        if (e.DataTransfer.Contains(ColumnDragFormat))
        {
            var draggedColumn = e.DataTransfer.TryGetValue(ColumnDragFormat);
            if (draggedColumn != null && draggedColumn != Column && !draggedColumn.IsPositionLocked)
            {
                UpdateDropIndicator(e);
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
            }
        }
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Don't allow dropping on locked columns
        if (!AllowReorder || Column == null || IsPositionLocked) return;

        if (e.DataTransfer.Contains(ColumnDragFormat))
        {
            var draggedColumn = e.DataTransfer.TryGetValue(ColumnDragFormat);
            if (draggedColumn != null && draggedColumn != Column && !draggedColumn.IsPositionLocked)
            {
                UpdateDropIndicator(e);
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
            }
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        IsDragOverLeft = false;
        IsDragOverRight = false;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        IsDragOverLeft = false;
        IsDragOverRight = false;

        // Don't allow dropping on locked columns
        if (!AllowReorder || Column == null || IsPositionLocked) return;

        if (e.DataTransfer.TryGetValue(ColumnDragFormat) is GridColumn draggedColumn && draggedColumn != Column && !draggedColumn.IsPositionLocked)
        {
            var position = e.GetPosition(this);
            var dropOnLeft = position.X < Bounds.Width / 2;

            var oldIndex = draggedColumn.VisibleIndex;
            var newIndex = Column.VisibleIndex;

            // Adjust index based on drop position
            if (!dropOnLeft && oldIndex < newIndex)
            {
                // Dropping on right side, keep same index
            }
            else if (dropOnLeft && oldIndex > newIndex)
            {
                // Dropping on left side, keep same index
            }
            else if (!dropOnLeft)
            {
                newIndex++;
            }

            if (oldIndex != newIndex)
            {
                RaiseEvent(new ColumnReorderEventArgs(ReorderCompletedEvent, draggedColumn, oldIndex, newIndex));
            }

            e.Handled = true;
        }
    }

    private void UpdateDropIndicator(DragEventArgs e)
    {
        var position = e.GetPosition(this);
        var dropOnLeft = position.X < Bounds.Width / 2;

        IsDragOverLeft = dropOnLeft;
        IsDragOverRight = !dropOnLeft;
    }

    #endregion

    #region Resize Grip

    private void OnResizeGripPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!AllowResize || Column == null) return;

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            // Double-clic sur le grip = ajuster la largeur au contenu
            if (e.ClickCount == 2 && AllowResize)
            {
                AutoFitWidth();
                e.Handled = true;
                return;
            }

            _isResizing = true;
            _resizeStartPoint = point.Position;
            _originalWidth = Column.ActualWidth > 0 ? Column.ActualWidth : Bounds.Width;
            e.Pointer.Capture(_resizeGrip);
            e.Handled = true;
        }
    }

    private void OnResizeGripPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing || Column == null) return;

        var point = e.GetCurrentPoint(this);
        var delta = point.Position.X - _resizeStartPoint.X;
        var newWidth = Math.Max(Column.MinWidth, Math.Min(Column.MaxWidth, _originalWidth + delta));

        Column.ActualWidth = newWidth;
        Column.Width = new GridLength(newWidth);

        e.Handled = true;
    }

    private void OnResizeGripPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isResizing || Column == null) return;

        _isResizing = false;
        e.Pointer.Capture(null);

        RaiseEvent(new ColumnResizeEventArgs(ResizeCompletedEvent, Column, _originalWidth, Column.ActualWidth));
        e.Handled = true;
    }

    private void OnFilterButtonClick(object? sender, RoutedEventArgs e)
    {
        if (Column != null)
        {
            RaiseEvent(new ColumnEventArgs(FilterRequestedEvent, Column));
        }
    }

    private void OnPinButtonClick(object? sender, RoutedEventArgs e)
    {
        if (Column != null)
        {
            var freeze = !IsFrozen;
            RaiseEvent(new ColumnFreezeEventArgs(FreezeRequestedEvent, Column, freeze));
        }
    }

    private void AutoFitWidth()
    {
        // Le calcul nécessite les données : délégué à la grille via événement
        if (Column != null)
            RaiseEvent(new ColumnEventArgs(AutoFitRequestedEvent, Column));
    }

    #endregion

    #endregion
}

#region Event Args

public class ColumnResizeEventArgs : RoutedEventArgs
{
    public GridColumn Column { get; }
    public double OldWidth { get; }
    public double NewWidth { get; }

    public ColumnResizeEventArgs(RoutedEvent routedEvent, GridColumn column, double oldWidth, double newWidth)
        : base(routedEvent)
    {
        Column = column;
        OldWidth = oldWidth;
        NewWidth = newWidth;
    }
}

public class ColumnReorderEventArgs : RoutedEventArgs
{
    public GridColumn Column { get; }
    public int OldIndex { get; }
    public int NewIndex { get; }

    public ColumnReorderEventArgs(RoutedEvent routedEvent, GridColumn column, int oldIndex, int newIndex)
        : base(routedEvent)
    {
        Column = column;
        OldIndex = oldIndex;
        NewIndex = newIndex;
    }
}

public class ColumnFreezeEventArgs : RoutedEventArgs
{
    public GridColumn Column { get; }
    public bool Freeze { get; }

    public ColumnFreezeEventArgs(RoutedEvent routedEvent, GridColumn column, bool freeze)
        : base(routedEvent)
    {
        Column = column;
        Freeze = freeze;
    }
}

#endregion
