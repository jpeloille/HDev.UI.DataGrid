using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace HDev.UI.DataGrid.Helpers;

/// <summary>
/// Helper class for virtualization calculations.
/// </summary>
public static class VirtualizationHelper
{
    /// <summary>
    /// Calculates which items are visible in the viewport.
    /// </summary>
    public static (int StartIndex, int EndIndex) GetVisibleRange(
        double viewportHeight,
        double scrollOffset,
        double itemHeight,
        int totalItems)
    {
        if (totalItems == 0 || itemHeight <= 0)
            return (0, 0);

        var startIndex = Math.Max(0, (int)(scrollOffset / itemHeight) - 1);
        var visibleCount = (int)Math.Ceiling(viewportHeight / itemHeight) + 2;
        var endIndex = Math.Min(totalItems - 1, startIndex + visibleCount);

        return (startIndex, endIndex);
    }

    /// <summary>
    /// Calculates the total height needed for all items.
    /// </summary>
    public static double GetTotalHeight(int itemCount, double itemHeight)
    {
        return itemCount * itemHeight;
    }

    /// <summary>
    /// Gets the Y offset for an item at a given index.
    /// </summary>
    public static double GetItemOffset(int index, double itemHeight)
    {
        return index * itemHeight;
    }

    /// <summary>
    /// Finds the index of the item at a given Y position.
    /// </summary>
    public static int GetIndexAtPosition(double y, double itemHeight, int totalItems)
    {
        if (itemHeight <= 0 || totalItems == 0)
            return -1;

        var index = (int)(y / itemHeight);
        return Math.Clamp(index, 0, totalItems - 1);
    }
}

/// <summary>
/// Helper class for column width calculations.
/// </summary>
public static class ColumnWidthHelper
{
    /// <summary>
    /// Calculates actual widths for columns based on available width.
    /// </summary>
    public static void CalculateColumnWidths(
        IEnumerable<Models.GridColumn> columns,
        double availableWidth)
    {
        var columnList = columns.Where(c => c.IsVisible).ToList();
        if (!columnList.Any()) return;

        // First pass: assign fixed widths and calculate remaining space
        double usedWidth = 0;
        double totalStarWeight = 0;

        foreach (var column in columnList)
        {
            if (column.Width.IsAbsolute)
            {
                column.ActualWidth = Math.Clamp(column.Width.Value, column.MinWidth, column.MaxWidth);
                usedWidth += column.ActualWidth;
            }
            else if (column.Width.IsStar)
            {
                totalStarWeight += column.Width.Value;
            }
            else if (column.Width.IsAuto)
            {
                // Auto columns will be measured later
                column.ActualWidth = column.MinWidth;
                usedWidth += column.ActualWidth;
            }
        }

        // Second pass: distribute remaining space to star columns
        var remainingWidth = Math.Max(0, availableWidth - usedWidth);
        
        if (totalStarWeight > 0)
        {
            var widthPerStar = remainingWidth / totalStarWeight;

            foreach (var column in columnList.Where(c => c.Width.IsStar))
            {
                var desiredWidth = widthPerStar * column.Width.Value;
                column.ActualWidth = Math.Clamp(desiredWidth, column.MinWidth, column.MaxWidth);
            }
        }
    }

    /// <summary>
    /// Auto-fits a column width based on content.
    /// </summary>
    public static double MeasureColumnWidth(
        Models.GridColumn column,
        IEnumerable<object> items,
        Func<object, Models.GridColumn, double> measureFunc,
        double headerWidth)
    {
        var maxContentWidth = items
            .Take(100) // Limit for performance
            .Select(item => measureFunc(item, column))
            .DefaultIfEmpty(column.MinWidth)
            .Max();

        return Math.Max(headerWidth, maxContentWidth);
    }
}

/// <summary>
/// Helper for hit testing in the grid.
/// </summary>
public static class HitTestHelper
{
    /// <summary>
    /// Finds the cell at a given point.
    /// </summary>
    public static (int RowIndex, Models.GridColumn? Column) GetCellAtPoint(
        Point point,
        IReadOnlyList<Models.GridColumn> columns,
        double rowHeight,
        double headerHeight,
        double scrollOffsetX,
        double scrollOffsetY)
    {
        // Calculate row index
        var adjustedY = point.Y - headerHeight + scrollOffsetY;
        var rowIndex = adjustedY >= 0 ? (int)(adjustedY / rowHeight) : -1;

        // Calculate column
        var adjustedX = point.X + scrollOffsetX;
        double currentX = 0;
        Models.GridColumn? foundColumn = null;

        foreach (var column in columns.Where(c => c.IsVisible))
        {
            if (adjustedX >= currentX && adjustedX < currentX + column.ActualWidth)
            {
                foundColumn = column;
                break;
            }
            currentX += column.ActualWidth;
        }

        return (rowIndex, foundColumn);
    }

    /// <summary>
    /// Gets the bounds of a cell.
    /// </summary>
    public static Rect GetCellBounds(
        int rowIndex,
        Models.GridColumn column,
        IReadOnlyList<Models.GridColumn> columns,
        double rowHeight,
        double headerHeight)
    {
        double x = 0;
        foreach (var col in columns.Where(c => c.IsVisible))
        {
            if (col == column) break;
            x += col.ActualWidth;
        }

        var y = headerHeight + (rowIndex * rowHeight);

        return new Rect(x, y, column.ActualWidth, rowHeight);
    }
}

/// <summary>
/// Helper for clipboard operations.
/// </summary>
public static class ClipboardHelper
{
    /// <summary>
    /// Formats selected cells for clipboard.
    /// </summary>
    public static string FormatForClipboard(
        IEnumerable<object> items,
        IEnumerable<Models.GridColumn> columns,
        bool includeHeaders = true)
    {
        var sb = new System.Text.StringBuilder();
        var visibleColumns = columns.Where(c => c.IsVisible).ToList();

        // Headers
        if (includeHeaders)
        {
            sb.AppendLine(string.Join("\t", visibleColumns.Select(c => c.Header)));
        }

        // Data
        foreach (var item in items)
        {
            var values = visibleColumns.Select(c => c.GetCellValue(item)?.ToString() ?? string.Empty);
            sb.AppendLine(string.Join("\t", values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats selected cells as CSV.
    /// </summary>
    public static string FormatAsCsv(
        IEnumerable<object> items,
        IEnumerable<Models.GridColumn> columns,
        bool includeHeaders = true)
    {
        var sb = new System.Text.StringBuilder();
        var visibleColumns = columns.Where(c => c.IsVisible).ToList();

        // Headers
        if (includeHeaders)
        {
            sb.AppendLine(string.Join(",", visibleColumns.Select(c => EscapeCsvValue(c.Header))));
        }

        // Data
        foreach (var item in items)
        {
            var values = visibleColumns.Select(c => EscapeCsvValue(c.GetCellValue(item)?.ToString() ?? string.Empty));
            sb.AppendLine(string.Join(",", values));
        }

        return sb.ToString();
    }

    private static string EscapeCsvValue(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
