using System.Globalization;
using Avalonia.Media;

namespace Julien.Avalonia.DataGrid.Models;

/// <summary>
/// The kind of aggregate computed by a <see cref="GridSummary"/>.
/// </summary>
public enum SummaryType
{
    Count,
    Sum,
    Average,
    Min,
    Max
}

/// <summary>
/// Defines an aggregate to display for a column (in the total footer or, later,
/// per group).
/// </summary>
public class GridSummary
{
    /// <summary>The field to aggregate.</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>The aggregate kind.</summary>
    public SummaryType SummaryType { get; set; } = SummaryType.Count;

    /// <summary>Optional .NET format string applied to the aggregate value (e.g. "C0", "N2").</summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Optional caption wrapping the value, where <c>{0}</c> is the formatted
    /// aggregate (e.g. "Sum: {0}"). When null, only the value is shown.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Computes the aggregate over <paramref name="items"/>. Returns null when
    /// there is nothing to aggregate.
    /// </summary>
    public object? Compute(IEnumerable<object> items)
    {
        if (string.IsNullOrEmpty(FieldName))
            return SummaryType == SummaryType.Count ? items.Count() : null;

        switch (SummaryType)
        {
            case SummaryType.Count:
                return items.Count();

            case SummaryType.Sum:
            case SummaryType.Average:
            {
                double acc = 0;
                int n = 0;
                foreach (var item in items)
                {
                    if (TryGetDouble(item, out var d)) { acc += d; n++; }
                }
                if (n == 0) return null;
                return SummaryType == SummaryType.Sum ? acc : acc / n;
            }

            case SummaryType.Min:
            case SummaryType.Max:
            {
                object? best = null;
                foreach (var item in items)
                {
                    var value = Helpers.PropertyAccessor.GetValue(item, FieldName);
                    if (value == null) continue;
                    if (best == null)
                    {
                        best = value;
                        continue;
                    }
                    int cmp = Comparer<object>.Default.Compare(value, best);
                    if ((SummaryType == SummaryType.Min && cmp < 0) ||
                        (SummaryType == SummaryType.Max && cmp > 0))
                    {
                        best = value;
                    }
                }
                return best;
            }

            default:
                return null;
        }
    }

    /// <summary>
    /// Computes the aggregate and renders it as display text (format + caption).
    /// </summary>
    public string ComputeText(IEnumerable<object> items)
    {
        var value = Compute(items);
        if (value == null) return string.Empty;

        var formatted = !string.IsNullOrEmpty(FormatString)
            ? string.Format(CultureInfo.CurrentCulture, $"{{0:{FormatString}}}", value)
            : Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

        return Caption != null
            ? string.Format(CultureInfo.CurrentCulture, Caption, formatted)
            : formatted;
    }

    private bool TryGetDouble(object item, out double result)
    {
        result = 0;
        var value = Helpers.PropertyAccessor.GetValue(item, FieldName);
        if (value == null) return false;
        try
        {
            result = Convert.ToDouble(value, CultureInfo.CurrentCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// View-model for a single cell of the summary footer, aligned under a column.
/// </summary>
public class GridFooterCell
{
    public double Width { get; init; }
    public string Text { get; init; } = string.Empty;
    public TextAlignment TextAlignment { get; init; } = TextAlignment.Right;
}
