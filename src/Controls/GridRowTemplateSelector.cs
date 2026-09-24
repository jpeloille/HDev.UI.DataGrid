using Avalonia.Controls;
using Avalonia.Controls.Templates;
using HDev.UI.DataGrid.Models;

namespace HDev.UI.DataGrid;

/// <summary>
/// Selects the row template per item type for the grid's virtualized rows
/// presenter: <see cref="HDevDataGridGroupRow"/> for <see cref="GridGroup"/> headers,
/// otherwise the data-row template.
/// <para>
/// This is set as the ItemsControl's <c>ItemTemplate</c> (not added to its
/// <c>DataTemplates</c>) on purpose: templates in <c>DataTemplates</c> are
/// inherited by descendant content presenters, so a catch-all entry would also
/// match cell values and recurse. An <c>ItemTemplate</c> applies only to this
/// control's items.
/// </para>
/// </summary>
public class GridRowTemplateSelector : IDataTemplate
{
    /// <summary>Template used for <see cref="GridGroup"/> header rows.</summary>
    public IDataTemplate? GroupTemplate { get; set; }

    /// <summary>Template used for ordinary data rows.</summary>
    public IDataTemplate? RowTemplate { get; set; }

    public bool Match(object? data) => true;

    public Control? Build(object? data)
    {
        return data is GridGroup
            ? GroupTemplate?.Build(data)
            : RowTemplate?.Build(data);
    }
}
