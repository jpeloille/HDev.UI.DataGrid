using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Julien.Avalonia.DataGrid.Models;

namespace Julien.Avalonia.DataGrid.Controls;

/// <summary>
/// A filter cell control for the filter row: text input + operator selector
/// (the operator set depends on the column type).
/// </summary>
public class JDataGridFilterCell : TemplatedControl
{
    private TextBox? _filterTextBox;
    private Button? _operatorButton;
    private string _lastFilterText = string.Empty;

    #region Styled Properties

    public static readonly StyledProperty<GridColumn?> ColumnProperty =
        AvaloniaProperty.Register<JDataGridFilterCell, GridColumn?>(nameof(Column));

    public static readonly StyledProperty<string> FilterTextProperty =
        AvaloniaProperty.Register<JDataGridFilterCell, string>(nameof(FilterText), string.Empty);

    public static readonly StyledProperty<FilterOperator> OperatorProperty =
        AvaloniaProperty.Register<JDataGridFilterCell, FilterOperator>(nameof(Operator), FilterOperator.Contains);

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

    /// <summary>Opérateur de filtre courant de la colonne</summary>
    public FilterOperator Operator
    {
        get => GetValue(OperatorProperty);
        set => SetValue(OperatorProperty, value);
    }

    #endregion

    #region Operator catalog

    /// <summary>Vrai si l'opérateur n'attend pas de valeur saisie</summary>
    internal static bool IsValueless(FilterOperator op) => op is
        FilterOperator.IsNull or FilterOperator.IsNotNull or
        FilterOperator.Today or FilterOperator.ThisWeek or
        FilterOperator.ThisMonth or FilterOperator.ThisYear;

    private static readonly (FilterOperator Op, string Symbol, string Label)[] TextOperators =
    {
        (FilterOperator.Contains, "≈", "Contient"),
        (FilterOperator.NotContains, "≉", "Ne contient pas"),
        (FilterOperator.Equals, "=", "Égal à"),
        (FilterOperator.NotEquals, "≠", "Différent de"),
        (FilterOperator.StartsWith, "a…", "Commence par"),
        (FilterOperator.EndsWith, "…z", "Finit par"),
        (FilterOperator.IsNull, "∅", "Vide"),
        (FilterOperator.IsNotNull, "≠∅", "Non vide"),
    };

    private static readonly (FilterOperator Op, string Symbol, string Label)[] NumericOperators =
    {
        (FilterOperator.Equals, "=", "Égal à"),
        (FilterOperator.NotEquals, "≠", "Différent de"),
        (FilterOperator.GreaterThan, ">", "Supérieur à"),
        (FilterOperator.GreaterThanOrEqual, "≥", "Supérieur ou égal"),
        (FilterOperator.LessThan, "<", "Inférieur à"),
        (FilterOperator.LessThanOrEqual, "≤", "Inférieur ou égal"),
        (FilterOperator.IsNull, "∅", "Vide"),
        (FilterOperator.IsNotNull, "≠∅", "Non vide"),
    };

    private static readonly (FilterOperator Op, string Symbol, string Label)[] DateOperators =
    {
        (FilterOperator.Equals, "=", "Égal à"),
        (FilterOperator.GreaterThanOrEqual, "≥", "À partir de"),
        (FilterOperator.LessThanOrEqual, "≤", "Jusqu'à"),
        (FilterOperator.Today, "auj", "Aujourd'hui"),
        (FilterOperator.ThisWeek, "sem", "Cette semaine"),
        (FilterOperator.ThisMonth, "mois", "Ce mois-ci"),
        (FilterOperator.ThisYear, "an", "Cette année"),
        (FilterOperator.IsNull, "∅", "Vide"),
        (FilterOperator.IsNotNull, "≠∅", "Non vide"),
    };

    private static readonly (FilterOperator Op, string Symbol, string Label)[] BooleanOperators =
    {
        (FilterOperator.Equals, "=", "Égal à"),
        (FilterOperator.NotEquals, "≠", "Différent de"),
    };

    private (FilterOperator Op, string Symbol, string Label)[] GetOperatorsForColumn()
        => Column?.ColumnType switch
        {
            ColumnType.Numeric => NumericOperators,
            ColumnType.DateTime => DateOperators,
            ColumnType.Boolean => BooleanOperators,
            _ => TextOperators
        };

    private string GetSymbol(FilterOperator op)
    {
        foreach (var (candidate, symbol, _) in GetOperatorsForColumn())
        {
            if (candidate == op) return symbol;
        }
        return "≈";
    }

    #endregion

    #region Template

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_filterTextBox != null)
        {
            _filterTextBox.TextChanged -= OnFilterTextChanged;
        }

        _filterTextBox = e.NameScope.Find<TextBox>("PART_FilterTextBox");
        _operatorButton = e.NameScope.Find<Button>("PART_OperatorButton");

        if (_filterTextBox != null)
        {
            _filterTextBox.TextChanged += OnFilterTextChanged;
        }

        if (_operatorButton != null)
        {
            _operatorButton.Content = GetSymbol(Operator);
            _operatorButton.Flyout = BuildOperatorFlyout();
        }
    }

    private MenuFlyout BuildOperatorFlyout()
    {
        var flyout = new MenuFlyout();
        foreach (var (op, symbol, label) in GetOperatorsForColumn())
        {
            var item = new MenuItem { Header = $"{symbol}  {label}" };
            var captured = op;
            item.Click += (s, e) => SetOperator(captured);
            flyout.Items.Add(item);
        }
        return flyout;
    }

    private void SetOperator(FilterOperator op)
    {
        if (Operator == op) return;

        Operator = op;
        if (_operatorButton != null)
            _operatorButton.Content = GetSymbol(op);

        // Les opérateurs sans valeur désactivent la saisie et s'appliquent seuls
        if (_filterTextBox != null)
            _filterTextBox.IsEnabled = !IsValueless(op);

        RaiseFilterChanged();
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
            RaiseFilterChanged();
        }
    }

    private void RaiseFilterChanged()
    {
        if (Column == null) return;
        RaiseEvent(new FilterChangedEventArgs(FilterChangedEvent, Column, FilterText, Operator));
    }

    #endregion
}

#region Event Args

public class FilterChangedEventArgs : RoutedEventArgs
{
    public GridColumn Column { get; }
    public string FilterText { get; }
    public FilterOperator Operator { get; }

    public FilterChangedEventArgs(RoutedEvent routedEvent, GridColumn column, string filterText,
        FilterOperator filterOperator = FilterOperator.Contains)
        : base(routedEvent)
    {
        Column = column;
        FilterText = filterText;
        Operator = filterOperator;
    }
}

#endregion
