using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Julien.Avalonia.DataGrid.Models;

namespace Julien.Avalonia.DataGrid.Controls;

/// <summary>
/// A filter cell control for the filter row.
/// </summary>
public class JDataGridFilterCell : TemplatedControl
{
    private TextBox? _filterTextBox;
    private string _lastFilterText = string.Empty;

    #region Styled Properties

    public static readonly StyledProperty<GridColumn?> ColumnProperty =
        AvaloniaProperty.Register<JDataGridFilterCell, GridColumn?>(nameof(Column));

    public static readonly StyledProperty<string> FilterTextProperty =
        AvaloniaProperty.Register<JDataGridFilterCell, string>(nameof(FilterText), string.Empty);

    #endregion

    #region Routed Events

    public static readonly RoutedEvent<FilterChangedEventArgs> FilterChangedEvent =
        RoutedEvent.Register<JDataGridFilterCell, FilterChangedEventArgs>(
            nameof(FilterChanged), RoutingStrategies.Bubble);

    public event EventHandler<FilterChangedEventArgs>? FilterChanged
    {
        add => AddHandler(FilterChangedEvent, value);
        remove => RemoveHandler(FilterChangedEvent, value);
    }

    #endregion

    #region Properties

    public GridColumn? Column
    {
        get => GetValue(ColumnProperty);
        set => SetValue(ColumnProperty, value);
    }

    public string FilterText
    {
        get => GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    #endregion

    #region Template

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Unsubscribe from old TextBox
        if (_filterTextBox != null)
        {
            _filterTextBox.TextChanged -= OnFilterTextChanged;
        }

        _filterTextBox = e.NameScope.Find<TextBox>("PART_FilterTextBox");

        if (_filterTextBox != null)
        {
            _filterTextBox.TextChanged += OnFilterTextChanged;
        }
    }

    #endregion

    #region Event Handlers

    private void OnFilterTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_filterTextBox == null || Column == null) return;

        var newText = _filterTextBox.Text ?? string.Empty;

        // Only raise event if text actually changed
        if (newText != _lastFilterText)
        {
            _lastFilterText = newText;
            FilterText = newText;

            RaiseEvent(new FilterChangedEventArgs(FilterChangedEvent, Column, newText));
        }
    }

    #endregion
}

#region Event Args

public class FilterChangedEventArgs : RoutedEventArgs
{
    public GridColumn Column { get; }
    public string FilterText { get; }

    public FilterChangedEventArgs(RoutedEvent routedEvent, GridColumn column, string filterText)
        : base(routedEvent)
    {
        Column = column;
        FilterText = filterText;
    }
}

#endregion
