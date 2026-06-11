using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Julien.Avalonia.DataGrid.Controls;
using Julien.Avalonia.DataGrid.Models;

// Headless render smoke test: boots Avalonia without a display, renders a real
// JDataGrid (which applies the control template + resolves bindings — the layer
// the build and the model-level checks do NOT exercise), then drives grouping
// and expand/collapse. Exits non-zero on any exception or failed assertion.

int failed = 0;
void Check(string name, bool ok)
{
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}");
    if (!ok) failed++;
}

try
{
    AppBuilder.Configure<SmokeApp>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions())
        .SetupWithoutStarting();

    var data = new List<Emp>
    {
        new() { Name = "Alice", Department = "Engineering", Salary = 90000, Hired = new DateTime(2018, 3, 1), Active = true },
        new() { Name = "Bob", Department = "Engineering", Salary = 85000, Hired = new DateTime(2019, 7, 12), Active = false },
        new() { Name = "Carol", Department = "Sales", Salary = 70000, Hired = new DateTime(2020, 1, 5), Active = true },
        new() { Name = "Dan", Department = "Sales", Salary = 72000, Hired = new DateTime(2021, 11, 20), Active = true },
        new() { Name = "Eve", Department = "HR", Salary = 65000, Hired = new DateTime(2017, 9, 9), Active = false },
    };

    var grid = new JDataGrid
    {
        ItemsSource = data,
        AutoGenerateColumns = true,
        ShowGroupPanel = true,
    };

    var window = new Window { Width = 1000, Height = 700, Content = grid };
    window.Show();
    Dispatcher.UIThread.RunJobs();
    ForceLayout(window);

    // 1. Data rows render as JDataGridRow (the fallback DataTemplate must match,
    //    otherwise items would render as plain text -> zero rows).
    int dataRows = grid.GetVisualDescendants().OfType<JDataGridRow>().Count();
    Check($"ungrouped: data rows render as JDataGridRow (found {dataRows})", dataRows > 0);

    // 2. Group: header rows render as JDataGridGroupRow.
    grid.GroupBy(nameof(Emp.Department));
    Dispatcher.UIThread.RunJobs();
    ForceLayout(window);

    int groupRows = grid.GetVisualDescendants().OfType<JDataGridGroupRow>().Count();
    int dataRowsGrouped = grid.GetVisualDescendants().OfType<JDataGridRow>().Count();
    Check($"grouped: group headers render as JDataGridGroupRow (found {groupRows})", groupRows > 0);
    Check($"grouped: data rows still render (found {dataRowsGrouped})", dataRowsGrouped > 0);

    // 3. Collapse all -> data rows disappear, headers remain.
    grid.CollapseAllGroups();
    Dispatcher.UIThread.RunJobs();
    ForceLayout(window);

    int groupRowsCollapsed = grid.GetVisualDescendants().OfType<JDataGridGroupRow>().Count();
    int dataRowsCollapsed = grid.GetVisualDescendants().OfType<JDataGridRow>().Count();
    Check($"collapsed: headers remain (found {groupRowsCollapsed})", groupRowsCollapsed > 0);
    Check($"collapsed: data rows hidden (found {dataRowsCollapsed})", dataRowsCollapsed == 0);

    // 4. Expand all -> data rows come back.
    grid.ExpandAllGroups();
    Dispatcher.UIThread.RunJobs();
    ForceLayout(window);

    int dataRowsReexpanded = grid.GetVisualDescendants().OfType<JDataGridRow>().Count();
    Check($"re-expanded: data rows return (found {dataRowsReexpanded})", dataRowsReexpanded > 0);

    // 5. Edit cycle: ungroup, edit the Name cell of Alice, commit, verify write-back.
    grid.ClearGrouping();
    Dispatcher.UIThread.RunJobs();
    ForceLayout(window);

    var alice = data[0];
    var nameColumn = grid.Columns.GetByFieldName(nameof(Emp.Name));
    Check("Name column resolved + editable", nameColumn is { IsReadOnly: false });

    if (nameColumn != null)
    {
        grid.BeginEdit(alice, nameColumn);
        Dispatcher.UIThread.RunJobs();
        ForceLayout(window);

        var cell = grid.GetVisualDescendants().OfType<JDataGridCell>()
            .FirstOrDefault(c => Equals(c.RowData, alice) && c.Column == nameColumn);
        Check("edit: target cell entered editing state", cell is { IsEditing: true });

        var editor = cell?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        Check("edit: text editor materialized", editor != null);

        if (editor != null)
        {
            editor.Text = "Zoe";
            grid.CommitEdit();
            Dispatcher.UIThread.RunJobs();
            Check($"edit commit: value written back (Name='{alice.Name}')", alice.Name == "Zoe");
        }

        // Cancel: start another edit, change the editor, cancel -> value unchanged.
        grid.BeginEdit(alice, nameColumn);
        Dispatcher.UIThread.RunJobs();
        ForceLayout(window);
        var cell2 = grid.GetVisualDescendants().OfType<JDataGridCell>()
            .FirstOrDefault(c => Equals(c.RowData, alice) && c.Column == nameColumn);
        var editor2 = cell2?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        if (editor2 != null)
        {
            editor2.Text = "ShouldNotStick";
            grid.CancelEdit();
            Dispatcher.UIThread.RunJobs();
            Check($"edit cancel: value preserved (Name='{alice.Name}')", alice.Name == "Zoe");
        }
    }

    // 6. Row recycle: changing a row's DataContext must update the displayed cell
    //    value by rebinding, reusing the same cell instances (no rebuild churn).
    var recycleRow = new JDataGridRow { Columns = grid.Columns };
    var recycleWindow = new Window { Width = 900, Height = 60, Content = recycleRow };
    recycleWindow.Show();
    recycleRow.DataContext = data[1]; // Bob
    Dispatcher.UIThread.RunJobs();
    ForceLayout(recycleWindow);
    var cellBefore = recycleRow.GetVisualDescendants().OfType<JDataGridCell>().FirstOrDefault();
    var textBefore = cellBefore?.DisplayText;

    recycleRow.DataContext = data[2]; // Carol
    Dispatcher.UIThread.RunJobs();
    ForceLayout(recycleWindow);
    var cellAfter = recycleRow.GetVisualDescendants().OfType<JDataGridCell>().FirstOrDefault();
    var textAfter = cellAfter?.DisplayText;

    Check($"recycle: cell value rebinds on DataContext change ('{textBefore}' -> '{textAfter}')",
        textBefore != textAfter && !string.IsNullOrEmpty(textAfter));
    Check("recycle: same cell instances reused (no rebuild)",
        cellBefore != null && ReferenceEquals(cellBefore, cellAfter));
    recycleWindow.Close();

    // 7. RefreshCells reflects a structural column change on an already-realized
    //    row (the safety net for reorder/freeze/visibility now that recycle no
    //    longer rebuilds cells).
    var structRow = new JDataGridRow { Columns = grid.Columns };
    var structWindow = new Window { Width = 900, Height = 60, Content = structRow };
    structWindow.Show();
    structRow.DataContext = data[0];
    Dispatcher.UIThread.RunJobs();
    ForceLayout(structWindow);
    int cellsBeforeHide = structRow.GetVisualDescendants().OfType<JDataGridCell>().Count();

    grid.Columns[0].IsVisible = false;
    grid.Columns.UpdateVisibleIndices();
    structRow.RefreshCells();
    Dispatcher.UIThread.RunJobs();
    ForceLayout(structWindow);
    int cellsAfterHide = structRow.GetVisualDescendants().OfType<JDataGridCell>().Count();
    Check($"RefreshCells reflects hidden column ({cellsBeforeHide} -> {cellsAfterHide})", cellsAfterHide == cellsBeforeHide - 1);

    grid.Columns[0].IsVisible = true;
    grid.Columns.UpdateVisibleIndices();
    structWindow.Close();

    // 8. Summary engine + footer rendering.
    var allItems = data.Cast<object>().ToList();
    var sumSalary = new GridSummary { FieldName = nameof(Emp.Salary), SummaryType = SummaryType.Sum, FormatString = "N0", Caption = "Σ {0}" };
    var avgSalary = new GridSummary { FieldName = nameof(Emp.Salary), SummaryType = SummaryType.Average };
    var countNames = new GridSummary { FieldName = nameof(Emp.Name), SummaryType = SummaryType.Count };
    var maxHired = new GridSummary { FieldName = nameof(Emp.Hired), SummaryType = SummaryType.Max };

    Check("summary engine: Sum salaries == 382000", Convert.ToDouble(sumSalary.Compute(allItems)) == 382000d);
    Check("summary engine: Average salary == 76400", Convert.ToDouble(avgSalary.Compute(allItems)) == 76400d);
    Check("summary engine: Count == 5", Convert.ToInt32(countNames.Compute(allItems)) == 5);
    Check("summary engine: Max hire date", (DateTime)maxHired.Compute(allItems)! == new DateTime(2021, 11, 20));

    grid.TotalSummaries.Add(sumSalary);
    grid.ShowSummaryFooter = true;
    Dispatcher.UIThread.RunJobs();
    ForceLayout(window);

    bool footerRendersSum = grid.GetVisualDescendants().OfType<TextBlock>()
        .Any(t => t.Text != null && t.Text.Contains("382"));
    Check("summary footer: renders formatted salary sum (Σ 382,000)", footerRendersSum);

    // 9. Group summaries: per-group aggregate shown in the group header.
    grid.GroupSummaries.Add(new GridSummary { FieldName = nameof(Emp.Salary), SummaryType = SummaryType.Sum, FormatString = "N0", Caption = "Σ {0}" });
    grid.GroupBy(nameof(Emp.Department));
    Dispatcher.UIThread.RunJobs();
    ForceLayout(window);

    var groupSummaryTexts = grid.GetVisualDescendants().OfType<JDataGridGroupRow>()
        .Select(g => g.SummaryText).ToList();
    // Engineering = 90000 (Zoe) + 85000 (Bob) = 175000.
    Check($"group summary: Engineering shows Σ 175,000 (got [{string.Join(", ", groupSummaryTexts)}])",
        groupSummaryTexts.Any(t => t.Contains("175")));

    // --- Column-count scaling measurement (informs whether true column
    //     virtualization is warranted; absolute headless ms are indicative). ---
    Console.WriteLine();
    Console.WriteLine("Column-count scaling (initial layout):");
    var fields = new[] { nameof(Emp.Name), nameof(Emp.Department), nameof(Emp.Salary), nameof(Emp.Hired), nameof(Emp.Active) };
    var manyRows = Enumerable.Range(0, 200).Select(i => new Emp
    {
        Name = "N" + i, Department = fields[i % 3], Salary = 1000 * i, Hired = new DateTime(2010, 1, 1).AddDays(i), Active = i % 2 == 0
    }).ToList();

    foreach (var n in new[] { 10, 30, 60, 100 })
    {
        var cols = new GridColumnCollection();
        for (int i = 0; i < n; i++)
            cols.Add(new GridColumn { FieldName = fields[i % fields.Length], Header = "C" + i, Width = new GridLength(100) });
        cols.UpdateVisibleIndices();

        var g = new JDataGrid { AutoGenerateColumns = false, Columns = cols, ItemsSource = manyRows };
        var w = new Window { Width = 1200, Height = 600, Content = g };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        w.Show();
        ForceLayout(w);
        sw.Stop();

        int cells = g.GetVisualDescendants().OfType<JDataGridCell>().Count();
        Console.WriteLine($"  cols={n,3}: initial layout {sw.ElapsedMilliseconds,5} ms, cells realized = {cells}");
        w.Close();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"  [FAIL] unhandled exception: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex);
    failed++;
}

Console.WriteLine(failed == 0 ? "Headless smoke: ALL PASS" : $"Headless smoke: {failed} FAILED");
Environment.ExitCode = failed == 0 ? 0 : 1;

static void ForceLayout(Window window)
{
    window.Measure(new Size(window.Width, window.Height));
    window.Arrange(new Rect(0, 0, window.Width, window.Height));
    Dispatcher.UIThread.RunJobs();
}

public class SmokeApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://HeadlessSmoke"))
        {
            Source = new Uri("avares://Julien.Avalonia.DataGrid/Themes/Index.axaml")
        });
    }
}

public class Emp
{
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int Salary { get; set; }
    public DateTime Hired { get; set; }
    public bool Active { get; set; }
}
