using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Julien.Avalonia.DataGrid.Models;

/// <summary>
/// Represents a filter condition for a column.
/// </summary>
public class GridFilter
{
    public string FieldName { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; } = FilterOperator.Contains;
    public object? Value { get; set; }
    public object? Value2 { get; set; } // For Between operator
    public bool IsCaseSensitive { get; set; } = false;

    /// <summary>
    /// Evaluates whether an item matches this filter.
    /// </summary>
    public bool Matches(object item)
    {
        var getter = Helpers.PropertyAccessor.GetGetter(item.GetType(), FieldName);
        if (getter == null) return true;

        var itemValue = getter(item);
        return EvaluateFilter(itemValue);
    }

    private bool EvaluateFilter(object? itemValue)
    {
        // Null handling
        if (itemValue == null)
        {
            return Operator switch
            {
                FilterOperator.IsNull => true,
                FilterOperator.IsNotNull => false,
                FilterOperator.Equals when Value == null => true,
                FilterOperator.NotEquals when Value != null => true,
                _ => false
            };
        }

        var valueless = Operator is FilterOperator.IsNull or FilterOperator.IsNotNull
            or FilterOperator.Today or FilterOperator.ThisWeek
            or FilterOperator.ThisMonth or FilterOperator.ThisYear;
        if (Value == null && !valueless)
        {
            return true; // No filter value set
        }

        return Operator switch
        {
            FilterOperator.Equals => CompareEquals(itemValue, Value),
            FilterOperator.NotEquals => !CompareEquals(itemValue, Value),
            FilterOperator.Contains => StringContains(itemValue, Value),
            FilterOperator.NotContains => !StringContains(itemValue, Value),
            FilterOperator.StartsWith => StringStartsWith(itemValue, Value),
            FilterOperator.EndsWith => StringEndsWith(itemValue, Value),
            FilterOperator.GreaterThan => Compare(itemValue, Value) > 0,
            FilterOperator.GreaterThanOrEqual => Compare(itemValue, Value) >= 0,
            FilterOperator.LessThan => Compare(itemValue, Value) < 0,
            FilterOperator.LessThanOrEqual => Compare(itemValue, Value) <= 0,
            FilterOperator.Between => Compare(itemValue, Value) >= 0 && Compare(itemValue, Value2) <= 0,
            FilterOperator.IsNull => false, // Already handled above
            FilterOperator.IsNotNull => true, // Already handled above
            FilterOperator.In => IsInCollection(itemValue, Value),
            FilterOperator.NotIn => !IsInCollection(itemValue, Value),
            FilterOperator.Regex => MatchesRegex(itemValue, Value),
            FilterOperator.Today => IsToday(itemValue),
            FilterOperator.ThisWeek => IsThisWeek(itemValue),
            FilterOperator.ThisMonth => IsThisMonth(itemValue),
            FilterOperator.ThisYear => IsThisYear(itemValue),
            _ => true
        };
    }

    private bool CompareEquals(object itemValue, object? filterValue)
    {
        if (filterValue == null) return false;

        if (itemValue is string s1 && filterValue is string s2)
        {
            return IsCaseSensitive
                ? s1.Equals(s2, StringComparison.Ordinal)
                : s1.Equals(s2, StringComparison.OrdinalIgnoreCase);
        }

        if (itemValue.Equals(filterValue)) return true;

        // Valeur de filtre saisie en texte (filter row) vs cellule typée :
        // convertir avant de comparer, comme Compare()
        try
        {
            var converted = Convert.ChangeType(filterValue, itemValue.GetType());
            return itemValue.Equals(converted);
        }
        catch
        {
            return false;
        }
    }

    private bool StringContains(object itemValue, object? filterValue)
    {
        var str = itemValue.ToString() ?? string.Empty;
        var filter = filterValue?.ToString() ?? string.Empty;
        
        return IsCaseSensitive
            ? str.Contains(filter, StringComparison.Ordinal)
            : str.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private bool StringStartsWith(object itemValue, object? filterValue)
    {
        var str = itemValue.ToString() ?? string.Empty;
        var filter = filterValue?.ToString() ?? string.Empty;
        
        return IsCaseSensitive
            ? str.StartsWith(filter, StringComparison.Ordinal)
            : str.StartsWith(filter, StringComparison.OrdinalIgnoreCase);
    }

    private bool StringEndsWith(object itemValue, object? filterValue)
    {
        var str = itemValue.ToString() ?? string.Empty;
        var filter = filterValue?.ToString() ?? string.Empty;
        
        return IsCaseSensitive
            ? str.EndsWith(filter, StringComparison.Ordinal)
            : str.EndsWith(filter, StringComparison.OrdinalIgnoreCase);
    }

    private int Compare(object itemValue, object? filterValue)
    {
        if (filterValue == null) return 1;
        
        if (itemValue is IComparable comparable)
        {
            try
            {
                var convertedFilter = Convert.ChangeType(filterValue, itemValue.GetType());
                return comparable.CompareTo(convertedFilter);
            }
            catch
            {
                return 0;
            }
        }
        
        return 0;
    }

    private bool IsInCollection(object itemValue, object? filterValue)
    {
        if (filterValue is IEnumerable<object> collection)
        {
            return collection.Any(v => CompareEquals(itemValue, v));
        }
        return false;
    }

    private bool MatchesRegex(object itemValue, object? filterValue)
    {
        var str = itemValue.ToString() ?? string.Empty;
        var pattern = filterValue?.ToString() ?? string.Empty;
        
        try
        {
            var options = IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            return Regex.IsMatch(str, pattern, options);
        }
        catch
        {
            return false;
        }
    }

    private bool IsToday(object itemValue)
    {
        if (itemValue is DateTime dt)
            return dt.Date == DateTime.Today;
        if (itemValue is DateTimeOffset dto)
            return dto.Date == DateTime.Today;
        return false;
    }

    private bool IsThisWeek(object itemValue)
    {
        DateTime date;
        if (itemValue is DateTime dt) date = dt;
        else if (itemValue is DateTimeOffset dto) date = dto.DateTime;
        else return false;

        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(7);
        
        return date >= startOfWeek && date < endOfWeek;
    }

    private bool IsThisMonth(object itemValue)
    {
        DateTime date;
        if (itemValue is DateTime dt) date = dt;
        else if (itemValue is DateTimeOffset dto) date = dto.DateTime;
        else return false;

        var today = DateTime.Today;
        return date.Year == today.Year && date.Month == today.Month;
    }

    private bool IsThisYear(object itemValue)
    {
        DateTime date;
        if (itemValue is DateTime dt) date = dt;
        else if (itemValue is DateTimeOffset dto) date = dto.DateTime;
        else return false;

        return date.Year == DateTime.Today.Year;
    }
}

/// <summary>
/// Available filter operators.
/// </summary>
public enum FilterOperator
{
    // Text/General
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    
    // Comparison
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    
    // Null checks
    IsNull,
    IsNotNull,
    
    // Collection
    In,
    NotIn,
    
    // Advanced
    Regex,
    
    // Date shortcuts
    Today,
    ThisWeek,
    ThisMonth,
    ThisYear
}

/// <summary>
/// Combines multiple filters with AND/OR logic.
/// </summary>
public class FilterGroup
{
    public List<GridFilter> Filters { get; } = new();
    public List<FilterGroup> SubGroups { get; } = new();
    public FilterLogic Logic { get; set; } = FilterLogic.And;

    public bool Matches(object item)
    {
        var filterResults = Filters.Select(f => f.Matches(item));
        var groupResults = SubGroups.Select(g => g.Matches(item));
        var allResults = filterResults.Concat(groupResults);

        return Logic switch
        {
            FilterLogic.And => allResults.All(r => r),
            FilterLogic.Or => allResults.Any(r => r),
            _ => true
        };
    }
}

public enum FilterLogic
{
    And,
    Or
}
