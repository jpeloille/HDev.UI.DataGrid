using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Julien.Avalonia.DataGrid.Models;

namespace Julien.Avalonia.DataGrid.Controls;

/// <summary>
/// Represents a single cell in the DataGrid.
/// </summary>
public class JDataGridCell : TemplatedControl
{
    #region Private Fields

    private ContentPresenter? _contentPresenter;
    private ContentPresenter? _editPresenter;
    private Control? _editor;
    private bool _isCancelling;

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<GridColumn?> ColumnProperty =
        AvaloniaProperty.Register<JDataGridCell, GridColumn?>(nameof(Column));

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<JDataGridCell, object?>(nameof(Value));

    public static readonly StyledProperty<object?> RowDataProperty =
        AvaloniaProperty.Register<JDataGridCell, object?>(nameof(RowData));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<JDataGridCell, bool>(nameof(IsSelected), false);

    public static readonly StyledProperty<bool> IsEditingProperty =
        AvaloniaProperty.Register<JDataGridCell, bool>(nameof(IsEditing), false);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<JDataGridCell, bool>(nameof(IsReadOnly), false);

    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        AvaloniaProperty.Register<JDataGridCell, TextAlignment>(nameof(TextAlignment), TextAlignment.Left);

    public static readonly StyledProperty<string?> DisplayTextProperty =
        AvaloniaProperty.Register<JDataGridCell, string?>(nameof(DisplayText));

    #endregion

    #region Routed Events

    public static readonly RoutedEvent<CellEventArgs> CellClickEvent =
        RoutedEvent.Register<JDataGridCell, CellEventArgs>(
            nameof(CellClick), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<CellEventArgs> CellDoubleClickEvent =
        RoutedEvent.Register<JDataGridCell, CellEventArgs>(
            nameof(CellDoubleClick), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<CellEventArgs> BeginEditEvent =
        RoutedEvent.Register<JDataGridCell, CellEventArgs>(
            nameof(BeginEdit), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<CellValueChangedEventArgs> ValueChangedEvent =
        RoutedEvent.Register<JDataGridCell, CellValueChangedEventArgs>(
            nameof(ValueChanged), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<CellEventArgs> EditEndedEvent =
        RoutedEvent.Register<JDataGridCell, CellEventArgs>(
            nameof(EditEnded), RoutingStrategies.Bubble);

    public event EventHandler<CellEventArgs>? CellClick
    {
        add => AddHandler(CellClickEvent, value);
        remove => RemoveHandler(CellClickEvent, value);
    }

    public event EventHandler<CellEventArgs>? CellDoubleClick
    {
        add => AddHandler(CellDoubleClickEvent, value);
        remove => RemoveHandler(CellDoubleClickEvent, value);
    }

    public event EventHandler<CellEventArgs>? BeginEdit
    {
        add => AddHandler(BeginEditEvent, value);
        remove => RemoveHandler(BeginEditEvent, value);
    }

    public event EventHandler<CellValueChangedEventArgs>? ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public event EventHandler<CellEventArgs>? EditEnded
    {
        add => AddHandler(EditEndedEvent, value);
        remove => RemoveHandler(EditEndedEvent, value);
    }

    #endregion

    #region Properties

    public GridColumn? Column
    {
        get => GetValue(ColumnProperty);
        set => SetValue(ColumnProperty, value);
    }

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public object? RowData
    {
        get => GetValue(RowDataProperty);
        set => SetValue(RowDataProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public string? DisplayText
    {
        get => GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    #endregion

    #region Pseudo Classes

    private static readonly string PC_Selected = ":selected";
    private static readonly string PC_Editing = ":editing";
    private static readonly string PC_ReadOnly = ":readonly";

    static JDataGridCell()
    {
        IsSelectedProperty.Changed.AddClassHandler<JDataGridCell>((cell, e) =>
            cell.PseudoClasses.Set(PC_Selected, (bool)e.NewValue!));

        IsEditingProperty.Changed.AddClassHandler<JDataGridCell>((cell, e) =>
        {
            cell.PseudoClasses.Set(PC_Editing, (bool)e.NewValue!);
            cell.OnEditingChanged((bool)e.NewValue!);
        });

        IsReadOnlyProperty.Changed.AddClassHandler<JDataGridCell>((cell, e) =>
            cell.PseudoClasses.Set(PC_ReadOnly, (bool)e.NewValue!));

        ColumnProperty.Changed.AddClassHandler<JDataGridCell>((cell, e) => cell.OnColumnChanged());
        ValueProperty.Changed.AddClassHandler<JDataGridCell>((cell, e) => cell.UpdateDisplayText());
        RowDataProperty.Changed.AddClassHandler<JDataGridCell>((cell, e) => cell.UpdateValue());
    }

    #endregion

    #region Template

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _editPresenter = e.NameScope.Find<ContentPresenter>("PART_EditPresenter");

        UpdateDisplayText();
    }

    #endregion

    #region Pointer Events

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            if (Column != null && RowData != null)
            {
                RaiseEvent(new CellEventArgs(CellClickEvent, RowData, Column));

                if (e.ClickCount == 2)
                {
                    RaiseEvent(new CellEventArgs(CellDoubleClickEvent, RowData, Column));

                    if (!IsReadOnly)
                    {
                        RaiseEvent(new CellEventArgs(BeginEditEvent, RowData, Column));
                    }
                }
            }
        }
    }

    #endregion

    #region Private Methods

    private void OnColumnChanged()
    {
        if (Column == null) return;

        TextAlignment = Column.TextAlignment;
        IsReadOnly = Column.IsReadOnly;

        UpdateValue();
    }

    private void UpdateValue()
    {
        if (Column == null || RowData == null)
        {
            Value = null;
            return;
        }

        Value = Column.GetCellValue(RowData);
        UpdateDisplayText();
    }

    private void UpdateDisplayText()
    {
        // Formatage partagé avec l'auto-fit et l'export (GridColumn.FormatValue)
        DisplayText = Models.GridColumn.FormatValue(Column, Value);
    }

    /// <summary>
    /// Commits the current edit (writes the editor value back) and ends editing.
    /// </summary>
    public void CommitEditing()
    {
        if (IsEditing) IsEditing = false;
    }

    /// <summary>
    /// Cancels the current edit without writing the editor value back.
    /// </summary>
    public void CancelEditing()
    {
        if (!IsEditing) return;
        _isCancelling = true;
        IsEditing = false;
    }

    private void OnEditingChanged(bool isEditing)
    {
        if (isEditing)
        {
            _isCancelling = false;
            StartEditing();
        }
        else
        {
            EndEditing();
        }
    }

    private void StartEditing()
    {
        if (Column == null || IsReadOnly) return;

        // Create appropriate editor based on column type
        _editor = CreateEditor();

        if (_editor != null && _editPresenter != null)
        {
            _editPresenter.Content = _editor;
            _editPresenter.IsVisible = true;

            if (_contentPresenter != null)
            {
                _contentPresenter.IsVisible = false;
            }

            // Focus the editor
            _editor.Focus();
        }
    }

    private void EndEditing()
    {
        if (_editor != null)
        {
            if (!_isCancelling)
            {
                // Get the edited value and write it back through the column
                // (which performs type/culture-aware conversion).
                var newValue = GetEditorValue();

                if (Column != null && RowData != null && Column.SetCellValue(RowData, newValue))
                {
                    var oldValue = Value;
                    Value = Column.GetCellValue(RowData);
                    UpdateDisplayText();

                    if (!Equals(oldValue, Value))
                    {
                        RaiseEvent(new CellValueChangedEventArgs(ValueChangedEvent, RowData, Column!, oldValue, Value));
                    }
                }
            }

            _editor = null;
        }

        if (_editPresenter != null)
        {
            _editPresenter.Content = null;
            _editPresenter.IsVisible = false;
        }

        if (_contentPresenter != null)
        {
            _contentPresenter.IsVisible = true;
        }

        _isCancelling = false;

        if (Column != null && RowData != null)
        {
            RaiseEvent(new CellEventArgs(EditEndedEvent, RowData, Column));
        }
    }

    private Control? CreateEditor()
    {
        if (Column == null) return null;

        // Use custom edit template if provided
        if (Column.EditTemplate != null)
        {
            return Column.EditTemplate.Build(RowData) as Control;
        }

        // Create default editor based on column type
        return Column.ColumnType switch
        {
            ColumnType.Boolean => CreateCheckBoxEditor(),
            ColumnType.Numeric => CreateNumericEditor(),
            ColumnType.DateTime => CreateDatePickerEditor(),
            ColumnType.ComboBox => CreateComboBoxEditor(),
            _ => CreateTextBoxEditor()
        };
    }

    private TextBox CreateTextBoxEditor()
    {
        var textBox = new TextBox
        {
            Text = Value?.ToString() ?? string.Empty,
            SelectionStart = 0,
            SelectionEnd = int.MaxValue
        };

        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                IsEditing = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelEditing();
                e.Handled = true;
            }
        };

        return textBox;
    }

    private CheckBox CreateCheckBoxEditor()
    {
        var checkBox = new CheckBox
        {
            IsChecked = Value as bool? ?? false,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center
        };

        checkBox.IsCheckedChanged += (s, e) =>
        {
            IsEditing = false;
        };

        return checkBox;
    }

    private NumericUpDown CreateNumericEditor()
    {
        var numericUpDown = new NumericUpDown
        {
            Value = Convert.ToDecimal(Value ?? 0),
            FormatString = Column?.FormatString ?? "N2"
        };

        numericUpDown.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                IsEditing = false;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelEditing();
                e.Handled = true;
            }
        };

        return numericUpDown;
    }

    private DatePicker CreateDatePickerEditor()
    {
        var datePicker = new DatePicker
        {
            // Value is typically a DateTime; `as DateTimeOffset?` would always be
            // null and open the picker at today instead of the cell's date.
            SelectedDate = Value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(dt),
                _ => null
            }
        };

        datePicker.SelectedDateChanged += (s, e) =>
        {
            IsEditing = false;
        };

        return datePicker;
    }

    private ComboBox CreateComboBoxEditor()
    {
        var comboBox = new ComboBox();

        // If the value type is an enum, populate with enum values
        if (Value != null && Value.GetType().IsEnum)
        {
            comboBox.ItemsSource = Enum.GetValues(Value.GetType());
            comboBox.SelectedItem = Value;
        }

        comboBox.SelectionChanged += (s, e) =>
        {
            IsEditing = false;
        };

        return comboBox;
    }

    private object? GetEditorValue()
    {
        return _editor switch
        {
            TextBox textBox => textBox.Text,
            CheckBox checkBox => checkBox.IsChecked,
            NumericUpDown numericUpDown => numericUpDown.Value,
            DatePicker datePicker => datePicker.SelectedDate?.DateTime,
            ComboBox comboBox => comboBox.SelectedItem,
            _ => Value
        };
    }

    #endregion
}

#region Event Args

public class CellEventArgs : RoutedEventArgs
{
    public object? Item { get; }
    public GridColumn Column { get; }

    public CellEventArgs(RoutedEvent routedEvent, object? item, GridColumn column)
        : base(routedEvent)
    {
        Item = item;
        Column = column;
    }
}

public class CellValueChangedEventArgs : RoutedEventArgs
{
    public object? Item { get; }
    public GridColumn Column { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public CellValueChangedEventArgs(
        RoutedEvent routedEvent,
        object? item,
        GridColumn column,
        object? oldValue,
        object? newValue)
        : base(routedEvent)
    {
        Item = item;
        Column = column;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

#endregion
