using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using System.ComponentModel;

namespace HDev.UI.DataGrid.Models;

/// <summary>
/// Defines a column in the DataGrid with full support for sorting, filtering,
/// grouping, editing, and custom templates.
/// </summary>
public class GridColumn : AvaloniaObject
{
    #region Static Properties

    public static readonly StyledProperty<string> FieldNameProperty =
        AvaloniaProperty.Register<GridColumn, string>(nameof(FieldName), string.Empty);

    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<GridColumn, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<GridLength> WidthProperty =
        AvaloniaProperty.Register<GridColumn, GridLength>(nameof(Width), new GridLength(100));

    public static readonly StyledProperty<double> MinWidthProperty =
        AvaloniaProperty.Register<GridColumn, double>(nameof(MinWidth), 50);

    public static readonly StyledProperty<double> MaxWidthProperty =
        AvaloniaProperty.Register<GridColumn, double>(nameof(MaxWidth), double.PositiveInfinity);

    public static readonly StyledProperty<bool> IsVisibleProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(IsVisible), true);

    public static readonly StyledProperty<bool> AllowSortProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(AllowSort), true);

    public static readonly StyledProperty<bool> AllowFilterProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(AllowFilter), true);

    public static readonly StyledProperty<bool> AllowGroupProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(AllowGroup), true);

    public static readonly StyledProperty<bool> AllowResizeProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(AllowResize), true);

    public static readonly StyledProperty<bool> AllowReorderProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(AllowReorder), true);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(IsReadOnly), false);

    public static readonly StyledProperty<bool> IsFrozenProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(IsFrozen), false);

    public static readonly StyledProperty<bool> IsPositionLockedProperty =
        AvaloniaProperty.Register<GridColumn, bool>(nameof(IsPositionLocked), false);

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        AvaloniaProperty.Register<GridColumn, TextAlignment>(nameof(TextAlignment), TextAlignment.Left);

    public static readonly StyledProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<GridColumn, IDataTemplate?>(nameof(CellTemplate));

    public static readonly StyledProperty<IDataTemplate?> EditTemplateProperty =
        AvaloniaProperty.Register<GridColumn, IDataTemplate?>(nameof(EditTemplate));

    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<GridColumn, IDataTemplate?>(nameof(HeaderTemplate));

    public static readonly StyledProperty<IDataTemplate?> FilterTemplateProperty =
        AvaloniaProperty.Register<GridColumn, IDataTemplate?>(nameof(FilterTemplate));

    public static readonly StyledProperty<string?> FormatStringProperty =
        AvaloniaProperty.Register<GridColumn, string?>(nameof(FormatString));

    public static readonly StyledProperty<ListSortDirection?> SortDirectionProperty =
        AvaloniaProperty.Register<GridColumn, ListSortDirection?>(nameof(SortDirection));

    public static readonly StyledProperty<int> SortIndexProperty =
        AvaloniaProperty.Register<GridColumn, int>(nameof(SortIndex), -1);

    public static readonly StyledProperty<ColumnType> ColumnTypeProperty =
        AvaloniaProperty.Register<GridColumn, ColumnType>(nameof(ColumnType), ColumnType.Text);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<GridColumn, IBrush?>(nameof(Background));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<GridColumn, IBrush?>(nameof(Foreground));

    public static readonly StyledProperty<double> ActualWidthProperty =
        AvaloniaProperty.Register<GridColumn, double>(nameof(ActualWidth), 100);

    #endregion

    #region Properties

    /// <summary>
    /// The property name to bind to in the data source.
    /// </summary>
    public string FieldName
    {
        get => (string)GetValue(FieldNameProperty)!;
        set => SetValue(FieldNameProperty, value);
    }

    /// <summary>
    /// The display text shown in the column header.
    /// </summary>
    public string Header
    {
        get => (string)GetValue(HeaderProperty)!;
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// The width of the column (supports Star, Auto, and Pixel values).
    /// </summary>
    public GridLength Width
    {
        get => (GridLength)GetValue(WidthProperty)!;
        set => SetValue(WidthProperty, value);
    }

    public double MinWidth
    {
        get => (double)GetValue(MinWidthProperty)!;
        set => SetValue(MinWidthProperty, value);
    }

    public double MaxWidth
    {
        get => (double)GetValue(MaxWidthProperty)!;
        set => SetValue(MaxWidthProperty, value);
    }

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty)!;
        set => SetValue(IsVisibleProperty, value);
    }

    public bool AllowSort
    {
        get => (bool)GetValue(AllowSortProperty)!;
        set => SetValue(AllowSortProperty, value);
    }

    public bool AllowFilter
    {
        get => (bool)GetValue(AllowFilterProperty)!;
        set => SetValue(AllowFilterProperty, value);
    }

    public bool AllowGroup
    {
        get => (bool)GetValue(AllowGroupProperty)!;
        set => SetValue(AllowGroupProperty, value);
    }

    public bool AllowResize
    {
        get => (bool)GetValue(AllowResizeProperty)!;
        set => SetValue(AllowResizeProperty, value);
    }

    public bool AllowReorder
    {
        get => (bool)GetValue(AllowReorderProperty)!;
        set => SetValue(AllowReorderProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty)!;
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// If true, this column stays visible when scrolling horizontally.
    /// </summary>
    public bool IsFrozen
    {
        get => (bool)GetValue(IsFrozenProperty)!;
        set => SetValue(IsFrozenProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the column position is locked (cannot be reordered).
    /// </summary>
    public bool IsPositionLocked
    {
        get => (bool)GetValue(IsPositionLockedProperty)!;
        set => SetValue(IsPositionLockedProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => (TextAlignment)GetValue(TextAlignmentProperty)!;
        set => SetValue(TextAlignmentProperty, value);
    }

    /// <summary>
    /// Custom template for displaying cell content.
    /// </summary>
    public IDataTemplate? CellTemplate
    {
        get => (IDataTemplate?)GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    /// <summary>
    /// Custom template for editing cell content.
    /// </summary>
    public IDataTemplate? EditTemplate
    {
        get => (IDataTemplate?)GetValue(EditTemplateProperty);
        set => SetValue(EditTemplateProperty, value);
    }

    /// <summary>
    /// Custom template for the column header.
    /// </summary>
    public IDataTemplate? HeaderTemplate
    {
        get => (IDataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>
    /// Custom template for the filter row.
    /// </summary>
    public IDataTemplate? FilterTemplate
    {
        get => (IDataTemplate?)GetValue(FilterTemplateProperty);
        set => SetValue(FilterTemplateProperty, value);
    }

    /// <summary>
    /// Format string for displaying values (e.g., "N2" for numbers, "d" for dates).
    /// </summary>
    public string? FormatString
    {
        get => (string?)GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    public ListSortDirection? SortDirection
    {
        get => (ListSortDirection?)GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }

    public int SortIndex
    {
        get => (int)GetValue(SortIndexProperty)!;
        set => SetValue(SortIndexProperty, value);
    }

    public ColumnType ColumnType
    {
        get => (ColumnType)GetValue(ColumnTypeProperty)!;
        set => SetValue(ColumnTypeProperty, value);
    }

    public IBrush? Background
    {
        get => (IBrush?)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? Foreground
    {
        get => (IBrush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the actual rendered width in pixels (computed by the grid from the
    /// Width mode — fixed/auto/star — and the available viewport). Cells, headers
    /// and footers bind to this so star/auto columns get real pixel widths.
    /// </summary>
    public double ActualWidth
    {
        get => GetValue(ActualWidthProperty);
        set => SetValue(ActualWidthProperty, value);
    }

    /// <summary>
    /// The visual index after reordering (different from definition order).
    /// </summary>
    public int VisibleIndex { get; internal set; }

    #endregion

    #region Methods

    /// <summary>
    /// Creates a binding for this column's field.
    /// </summary>
    public BindingBase CreateBinding()
    {
        var binding = new Binding(FieldName);
        
        if (!string.IsNullOrEmpty(FormatString))
        {
            binding.StringFormat = FormatString;
        }
        
        return binding;
    }

    // Cache the resolved getter for the last-seen row type so cell rendering
    // (the hot path) reuses a compiled delegate instead of doing a cache lookup
    // per access. Falls back to per-type resolution for polymorphic rows.
    // _fieldNameCache mirrors FieldName as a plain field to avoid the Avalonia
    // styled-property read cost on every cell access.
    private string _fieldNameCache = string.Empty;
    private Type? _getterType;
    private Func<object, object?>? _getter;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FieldNameProperty)
        {
            _fieldNameCache = FieldName ?? string.Empty;
            _getterType = null;
            _getter = null;
        }
    }

    /// <summary>
    /// Gets the value from a data item for this column.
    /// </summary>
    public object? GetCellValue(object item)
    {
        if (_fieldNameCache.Length == 0) return null;

        var type = item.GetType();
        if (type != _getterType)
        {
            _getter = Helpers.PropertyAccessor.GetGetter(type, _fieldNameCache);
            _getterType = type;
        }
        return _getter?.Invoke(item);
    }

    /// <summary>
    /// Sets the value on a data item for this column.
    /// </summary>
    public bool SetCellValue(object item, object? value)
    {
        if (string.IsNullOrEmpty(FieldName) || IsReadOnly) return false;
        return Helpers.PropertyAccessor.TrySetValue(item, FieldName, value);
    }

    /// <summary>
    /// Formats a raw cell value the way cells display it (FormatString,
    /// checkmark for bools, short date). Shared by cell rendering, auto-fit
    /// and export so they always agree.
    /// </summary>
    public static string FormatValue(GridColumn? column, object? value)
    {
        if (value == null) return string.Empty;

        if (column != null && !string.IsNullOrEmpty(column.FormatString))
            return string.Format($"{{0:{column.FormatString}}}", value);

        return value switch
        {
            bool b => b ? "✓" : "",
            DateTime d => d.ToShortDateString(),
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Gets the display text of this column for a data item.
    /// </summary>
    public string GetDisplayText(object item)
        => FormatValue(this, GetCellValue(item));

    #endregion
}

/// <summary>
/// Defines the type of column for automatic editor selection.
/// </summary>
public enum ColumnType
{
    Text,
    Numeric,
    DateTime,
    Boolean,
    ComboBox,
    Image,
    ProgressBar,
    Hyperlink,
    Button,
    Custom
}
