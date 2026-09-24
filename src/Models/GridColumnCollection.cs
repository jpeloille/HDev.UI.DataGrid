using Avalonia.Collections;

namespace HDev.UI.DataGrid.Models;

/// <summary>
/// Observable collection of GridColumn with additional helper methods.
/// </summary>
public class GridColumnCollection : AvaloniaList<GridColumn>
{
    public GridColumnCollection() : base()
    {
    }

    /// <summary>
    /// Gets a column by its field name.
    /// </summary>
    public GridColumn? GetByFieldName(string fieldName)
    {
        return this.FirstOrDefault(c => c.FieldName == fieldName);
    }

    /// <summary>
    /// Gets all visible columns ordered by their visual index.
    /// </summary>
    public IEnumerable<GridColumn> GetVisibleColumns()
    {
        return this.Where(c => c.IsVisible).OrderBy(c => c.VisibleIndex);
    }

    /// <summary>
    /// Gets all frozen columns.
    /// </summary>
    public IEnumerable<GridColumn> GetFrozenColumns()
    {
        return this.Where(c => c.IsFrozen && c.IsVisible).OrderBy(c => c.VisibleIndex);
    }

    /// <summary>
    /// Gets all non-frozen columns.
    /// </summary>
    public IEnumerable<GridColumn> GetScrollableColumns()
    {
        return this.Where(c => !c.IsFrozen && c.IsVisible).OrderBy(c => c.VisibleIndex);
    }

    /// <summary>
    /// Updates visible indices after reordering.
    /// </summary>
    public void UpdateVisibleIndices()
    {
        var visibleColumns = this.Where(c => c.IsVisible).ToList();
        for (int i = 0; i < visibleColumns.Count; i++)
        {
            visibleColumns[i].VisibleIndex = i;
        }
    }

    /// <summary>
    /// Moves a column to a new position.
    /// </summary>
    public void MoveColumn(GridColumn column, int newVisibleIndex)
    {
        if (!Contains(column)) return;

        var visibleColumns = this.Where(c => c.IsVisible).OrderBy(c => c.VisibleIndex).ToList();
        var currentIndex = visibleColumns.IndexOf(column);

        if (currentIndex < 0 || currentIndex == newVisibleIndex) return;

        visibleColumns.RemoveAt(currentIndex);

        if (newVisibleIndex > visibleColumns.Count)
            newVisibleIndex = visibleColumns.Count;

        visibleColumns.Insert(newVisibleIndex, column);

        // Update all visible indices
        for (int i = 0; i < visibleColumns.Count; i++)
        {
            visibleColumns[i].VisibleIndex = i;
        }
    }
}
