using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using HDev.UI.DataGrid.Models;

namespace HDev.UI.DataGrid;

/// <summary>
/// Represents a group row header in the DataGrid.
/// </summary>
public class HDevDataGridGroupRow : TemplatedControl
{
    #region Private Fields

    private ToggleButton? _expandButton;
    private ItemsControl? _childrenPresenter;

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<GridGroup?> GroupProperty =
        AvaloniaProperty.Register<HDevDataGridGroupRow, GridGroup?>(nameof(Group));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<HDevDataGridGroupRow, bool>(nameof(IsExpanded), true);

    public static readonly StyledProperty<int> LevelProperty =
        AvaloniaProperty.Register<HDevDataGridGroupRow, int>(nameof(Level), 0);

    public static readonly StyledProperty<string> DisplayTextProperty =
        AvaloniaProperty.Register<HDevDataGridGroupRow, string>(nameof(DisplayText), string.Empty);

    public static readonly StyledProperty<string> SummaryTextProperty =
        AvaloniaProperty.Register<HDevDataGridGroupRow, string>(nameof(SummaryText), string.Empty);

    public static readonly StyledProperty<int> ItemCountProperty =
        AvaloniaProperty.Register<HDevDataGridGroupRow, int>(nameof(ItemCount), 0);

    public static readonly StyledProperty<double> IndentWidthProperty =
        AvaloniaProperty.Register<HDevDataGridGroupRow, double>(nameof(IndentWidth), 20);

    public static readonly StyledProperty<double> RowHeightProperty =
        AvaloniaProperty.Register<HDevDataGridGroupRow, double>(nameof(RowHeight), 32);

    #endregion

    #region Routed Events

    public static readonly RoutedEvent<GroupEventArgs> ExpandChangedEvent =
        RoutedEvent.Register<HDevDataGridGroupRow, GroupEventArgs>(
            nameof(ExpandChanged), RoutingStrategies.Bubble);

    public event EventHandler<GroupEventArgs>? ExpandChanged
    {
        add => AddHandler(ExpandChangedEvent, value);
        remove => RemoveHandler(ExpandChangedEvent, value);
    }

    #endregion

    #region Properties

    public GridGroup? Group
    {
        get => GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public int Level
    {
        get => GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public string DisplayText
    {
        get => GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public string SummaryText
    {
        get => GetValue(SummaryTextProperty);
        set => SetValue(SummaryTextProperty, value);
    }

    public int ItemCount
    {
        get => GetValue(ItemCountProperty);
        set => SetValue(ItemCountProperty, value);
    }

    public double IndentWidth
    {
        get => GetValue(IndentWidthProperty);
        set => SetValue(IndentWidthProperty, value);
    }

    public double RowHeight
    {
        get => GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    /// <summary>
    /// Calculated indent based on level.
    /// </summary>
    public double TotalIndent => Level * IndentWidth;

    #endregion

    #region Pseudo Classes

    private static readonly string PC_Expanded = ":expanded";
    private static readonly string PC_Collapsed = ":collapsed";

    static HDevDataGridGroupRow()
    {
        IsExpandedProperty.Changed.AddClassHandler<HDevDataGridGroupRow>((row, e) =>
        {
            var isExpanded = (bool)e.NewValue!;
            row.PseudoClasses.Set(PC_Expanded, isExpanded);
            row.PseudoClasses.Set(PC_Collapsed, !isExpanded);
            row.OnExpandedChanged(isExpanded);
        });

        GroupProperty.Changed.AddClassHandler<HDevDataGridGroupRow>((row, e) => row.OnGroupChanged());
    }

    #endregion

    #region Template

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _expandButton = e.NameScope.Find<ToggleButton>("PART_ExpandButton");
        _childrenPresenter = e.NameScope.Find<ItemsControl>("PART_ChildrenPresenter");

        if (_expandButton != null)
        {
            _expandButton.IsChecked = IsExpanded;
            _expandButton.Click += OnExpandButtonClick;
        }

        UpdateFromGroup();
    }

    #endregion

    #region Event Handlers

    private void OnExpandButtonClick(object? sender, RoutedEventArgs e)
    {
        IsExpanded = _expandButton?.IsChecked ?? false;
    }

    private GridGroup? _subscribedGroup;

    private void OnGroupChanged()
    {
        if (_subscribedGroup != null)
            _subscribedGroup.PropertyChanged -= OnGroupPropertyChanged;

        _subscribedGroup = Group;

        if (_subscribedGroup != null)
            _subscribedGroup.PropertyChanged += OnGroupPropertyChanged;

        UpdateFromGroup();
    }

    private void OnGroupPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // SummaryText is computed by the grid after the group is built, so reflect
        // late updates here.
        UpdateFromGroup();
    }

    private void UpdateFromGroup()
    {
        if (Group == null) return;

        Level = Group.Level;
        DisplayText = Group.DisplayText;
        SummaryText = Group.SummaryText;
        ItemCount = Group.TotalItemCount;
        IsExpanded = Group.IsExpanded;

        // The :expanded/:collapsed pseudo-classes are otherwise only set on a
        // *change* of IsExpanded; since the default is already true, set them
        // explicitly so the chevron shows on first render.
        PseudoClasses.Set(PC_Expanded, IsExpanded);
        PseudoClasses.Set(PC_Collapsed, !IsExpanded);
    }

    private void OnExpandedChanged(bool isExpanded)
    {
        if (Group != null)
        {
            Group.IsExpanded = isExpanded;
            RaiseEvent(new GroupEventArgs(ExpandChangedEvent, Group, isExpanded));
        }

        UpdateChildrenVisibility();
    }

    private void UpdateChildrenVisibility()
    {
        if (_childrenPresenter != null)
        {
            _childrenPresenter.IsVisible = IsExpanded;
        }
    }

    #endregion

    #region Pointer Events

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Double-click to toggle expansion
        if (e.ClickCount == 2)
        {
            IsExpanded = !IsExpanded;
            e.Handled = true;
        }
    }

    #endregion
}

#region Event Args

public class GroupEventArgs : RoutedEventArgs
{
    public GridGroup Group { get; }
    public bool IsExpanded { get; }

    public GroupEventArgs(RoutedEvent routedEvent, GridGroup group, bool isExpanded)
        : base(routedEvent)
    {
        Group = group;
        IsExpanded = isExpanded;
    }
}

#endregion
