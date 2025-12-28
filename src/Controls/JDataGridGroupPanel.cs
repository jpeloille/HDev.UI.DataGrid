using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Julien.Avalonia.DataGrid.Models;
using System.Collections.ObjectModel;

namespace Julien.Avalonia.DataGrid.Controls;

/// <summary>
/// Panel for drag-and-drop column grouping.
/// </summary>
public class JDataGridGroupPanel : TemplatedControl
{
    private Border? _dropZone;
    private ItemsControl? _groupedColumnsPresenter;

    #region Styled Properties

    public static readonly StyledProperty<ObservableCollection<GridColumn>> GroupedColumnsProperty =
        AvaloniaProperty.Register<JDataGridGroupPanel, ObservableCollection<GridColumn>>(
            nameof(GroupedColumns), new ObservableCollection<GridColumn>());

    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<JDataGridGroupPanel, string>(
            nameof(PlaceholderText), "Drag a column header here to group by that column");

    public static readonly StyledProperty<bool> IsDragOverProperty =
        AvaloniaProperty.Register<JDataGridGroupPanel, bool>(nameof(IsDragOver), false);

    #endregion

    #region Routed Events

    public static readonly RoutedEvent<GroupColumnEventArgs> ColumnGroupedEvent =
        RoutedEvent.Register<JDataGridGroupPanel, GroupColumnEventArgs>(
            nameof(ColumnGrouped), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<GroupColumnEventArgs> ColumnUngroupedEvent =
        RoutedEvent.Register<JDataGridGroupPanel, GroupColumnEventArgs>(
            nameof(ColumnUngrouped), RoutingStrategies.Bubble);

    public event EventHandler<GroupColumnEventArgs>? ColumnGrouped
    {
        add => AddHandler(ColumnGroupedEvent, value);
        remove => RemoveHandler(ColumnGroupedEvent, value);
    }

    public event EventHandler<GroupColumnEventArgs>? ColumnUngrouped
    {
        add => AddHandler(ColumnUngroupedEvent, value);
        remove => RemoveHandler(ColumnUngroupedEvent, value);
    }

    #endregion

    #region Properties

    public ObservableCollection<GridColumn> GroupedColumns
    {
        get => GetValue(GroupedColumnsProperty);
        set => SetValue(GroupedColumnsProperty, value);
    }

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public bool IsDragOver
    {
        get => GetValue(IsDragOverProperty);
        set => SetValue(IsDragOverProperty, value);
    }

    #endregion

    #region Constructor

    public JDataGridGroupPanel()
    {
        GroupedColumns = new ObservableCollection<GridColumn>();
    }

    #endregion

    #region Template

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _dropZone = e.NameScope.Find<Border>("PART_DropZone");
        _groupedColumnsPresenter = e.NameScope.Find<ItemsControl>("PART_GroupedColumnsPresenter");

        // Enable drag-drop on this control
        DragDrop.SetAllowDrop(this, true);

        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    #endregion

    #region Drag/Drop Handlers

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains("GridColumn"))
        {
            IsDragOver = true;
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        IsDragOver = false;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        IsDragOver = false;

        if (e.Data.Get("GridColumn") is GridColumn column)
        {
            // Check if column is already grouped
            if (!GroupedColumns.Contains(column) && column.AllowGroup)
            {
                GroupedColumns.Add(column);
                RaiseEvent(new GroupColumnEventArgs(ColumnGroupedEvent, column));
            }

            e.Handled = true;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Removes a column from the grouping.
    /// </summary>
    public void RemoveGroup(GridColumn column)
    {
        if (GroupedColumns.Remove(column))
        {
            RaiseEvent(new GroupColumnEventArgs(ColumnUngroupedEvent, column));
        }
    }

    /// <summary>
    /// Clears all grouping.
    /// </summary>
    public void ClearGroups()
    {
        var columns = GroupedColumns.ToList();
        GroupedColumns.Clear();

        foreach (var column in columns)
        {
            RaiseEvent(new GroupColumnEventArgs(ColumnUngroupedEvent, column));
        }
    }

    #endregion
}

#region Event Args

public class GroupColumnEventArgs : RoutedEventArgs
{
    public GridColumn Column { get; }

    public GroupColumnEventArgs(RoutedEvent routedEvent, GridColumn column)
        : base(routedEvent)
    {
        Column = column;
    }
}

#endregion
