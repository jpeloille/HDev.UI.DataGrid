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
