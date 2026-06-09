using System.Diagnostics;
using Julien.Avalonia.DataGrid.Models;
using Julien.Avalonia.DataGrid.Helpers;

// Micro-benchmark: sort + filter pass through GridDataSource over a large dataset.
// Used to baseline the reflection-based access path and to verify the gain after
// switching to compiled property accessors (§1.1).

const int RowCount = 100_000;
const int Iterations = 5;

var rng = new Random(12345);
var depts = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance", "Support" };
var data = new List<Row>(RowCount);
for (int i = 0; i < RowCount; i++)
{
    data.Add(new Row
    {
        Id = i,
        Name = "Employee_" + rng.Next(0, 1_000_000),
        Department = depts[rng.Next(depts.Length)],
        Salary = 30_000 + rng.Next(0, 120_000),
        HireDate = new DateTime(2000, 1, 1).AddDays(rng.Next(0, 9000)),
        IsActive = rng.Next(2) == 0
    });
}

Console.WriteLine($"Rows: {RowCount:N0}, iterations: {Iterations}");

// Warm up.
RunPass(data);

var swSortOnly = new Stopwatch();
var swFilterOnly = new Stopwatch();
var swCombined = new Stopwatch();

for (int it = 0; it < Iterations; it++)
{
    // Sort only.
    {
        var ds = new GridDataSource { Source = data };
        swSortOnly.Start();
        ds.AddSort(nameof(Row.Salary), System.ComponentModel.ListSortDirection.Descending);
        ds.AddSort(nameof(Row.Name), System.ComponentModel.ListSortDirection.Ascending, append: true);
        _ = ds.View.Count;
        swSortOnly.Stop();
    }

    // Filter only.
    {
        var ds = new GridDataSource { Source = data };
        swFilterOnly.Start();
        ds.AddFilter(new GridFilter { FieldName = nameof(Row.Department), Operator = FilterOperator.Equals, Value = "Engineering" });
        ds.AddFilter(new GridFilter { FieldName = nameof(Row.Salary), Operator = FilterOperator.GreaterThan, Value = 60_000 });
        _ = ds.View.Count;
        swFilterOnly.Stop();
    }

    // Combined sort + filter (full refresh).
    {
        var ds = new GridDataSource { Source = data };
        swCombined.Start();
        ds.AddFilter(new GridFilter { FieldName = nameof(Row.Salary), Operator = FilterOperator.GreaterThan, Value = 50_000 });
        ds.AddSort(nameof(Row.Department), System.ComponentModel.ListSortDirection.Ascending);
        ds.AddSort(nameof(Row.Salary), System.ComponentModel.ListSortDirection.Descending, append: true);
        _ = ds.View.Count;
        swCombined.Stop();
    }
}

void RunPass(List<Row> rows)
{
    var ds = new GridDataSource { Source = rows };
    ds.AddFilter(new GridFilter { FieldName = nameof(Row.Salary), Operator = FilterOperator.GreaterThan, Value = 50_000 });
    ds.AddSort(nameof(Row.Salary), System.ComponentModel.ListSortDirection.Descending);
    _ = ds.View.Count;
}

Console.WriteLine($"Sort only (2 keys)       : {swSortOnly.ElapsedMilliseconds / (double)Iterations,8:F1} ms/pass");
Console.WriteLine($"Filter only (2 filters)  : {swFilterOnly.ElapsedMilliseconds / (double)Iterations,8:F1} ms/pass");
Console.WriteLine($"Combined sort+filter     : {swCombined.ElapsedMilliseconds / (double)Iterations,8:F1} ms/pass");

// --- Cell value access hot path (rendering/scroll) ---
// 100k rows x 6 properties read repeatedly: this is where compiled accessors
// replace PropertyInfo.GetValue. Direct A/B so the gain is not masked by sort cost.
const int AccessReads = 3;
var props = typeof(Row).GetProperties();

var swReflection = new Stopwatch();
swReflection.Start();
long sink1 = 0;
for (int r = 0; r < AccessReads; r++)
    foreach (var row in data)
        foreach (var p in props)
            sink1 += p.GetValue(row)?.GetHashCode() ?? 0;
swReflection.Stop();

var swCompiled = new Stopwatch();
swCompiled.Start();
long sink2 = 0;
for (int r = 0; r < AccessReads; r++)
    foreach (var row in data)
        foreach (var p in props)
            sink2 += PropertyAccessor.GetValue(row, p.Name)?.GetHashCode() ?? 0;
swCompiled.Stop();

// Column-cached path: how GridColumn.GetCellValue actually accesses values
// (getter resolved once per row type, reused per cell). This is the real hot path.
var columns = props.Select(p => new GridColumn { FieldName = p.Name }).ToArray();
var swColumn = new Stopwatch();
swColumn.Start();
long sink3 = 0;
for (int r = 0; r < AccessReads; r++)
    foreach (var row in data)
        foreach (var c in columns)
            sink3 += c.GetCellValue(row)?.GetHashCode() ?? 0;
swColumn.Stop();

var perReflection = swReflection.Elapsed.TotalMilliseconds / AccessReads;
var perCompiled = swCompiled.Elapsed.TotalMilliseconds / AccessReads;
var perColumn = swColumn.Elapsed.TotalMilliseconds / AccessReads;
Console.WriteLine();
Console.WriteLine($"Cell reads (100k x6) raw reflection      : {perReflection,8:F1} ms/pass");
Console.WriteLine($"Cell reads (100k x6) accessor (uncached) : {perCompiled,8:F1} ms/pass  (x{perReflection / Math.Max(perCompiled, 0.001):F1})");
Console.WriteLine($"Cell reads (100k x6) column-cached getter: {perColumn,8:F1} ms/pass  (x{perReflection / Math.Max(perColumn, 0.001):F1})");
if (sink1 != sink2 || sink1 != sink3) Console.WriteLine("WARNING: results diverged!");

// --- Visual-rows flattening correctness (task §1.4/§3.1) ---
Console.WriteLine();
Console.WriteLine("Visual-rows flattening checks:");
int passed = 0, failed = 0;
void Check(string name, bool ok)
{
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}");
    if (ok) passed++; else failed++;
}

var sample = new List<Row>
{
    new() { Id = 1, Department = "A", IsActive = true },
    new() { Id = 2, Department = "A", IsActive = false },
    new() { Id = 3, Department = "A", IsActive = true },
    new() { Id = 4, Department = "B", IsActive = true },
    new() { Id = 5, Department = "B", IsActive = false },
};

// Ungrouped: visual rows == data rows.
var g0 = new GridDataSource { Source = sample };
Check("ungrouped -> 5 data rows, no GridGroup", g0.VisualRows.Count == 5 && !g0.VisualRows.OfType<GridGroup>().Any());

// Single-level group by Department: [A, i, i, i, B, i, i] = 7 rows, 2 headers.
var g1 = new GridDataSource { Source = sample };
g1.AddGroup(nameof(Row.Department));
Check("group by Dept -> 7 visual rows", g1.VisualRows.Count == 7);
Check("group by Dept -> 2 group headers", g1.VisualRows.OfType<GridGroup>().Count() == 2);
Check("first visual row is a GridGroup", g1.VisualRows[0] is GridGroup);

// Collapse first group -> its 3 items vanish: [A(collapsed), B, i, i] = 4 rows.
var firstGroup = (GridGroup)g1.VisualRows[0];
firstGroup.IsExpanded = false;
g1.RebuildVisualRows();
Check("collapse group A -> 4 visual rows", g1.VisualRows.Count == 4);
Check("collapsed group still present as header", g1.VisualRows.OfType<GridGroup>().Count() == 2);

// Re-expand -> back to 7.
firstGroup.IsExpanded = true;
g1.RebuildVisualRows();
Check("re-expand group A -> 7 visual rows", g1.VisualRows.Count == 7);

// Two-level group by Department then IsActive.
// A -> {true:2, false:1}, B -> {true:1, false:1}
// rows: A, A/true, i, i, A/false, i, B, B/true, i, B/false, i
// = 2 top headers + 4 sub headers + 5 items = 11
var g2 = new GridDataSource { Source = sample };
g2.AddGroup(nameof(Row.Department));
g2.AddGroup(nameof(Row.IsActive));
Check("two-level group -> 11 visual rows", g2.VisualRows.Count == 11);
Check("two-level group -> 6 group headers", g2.VisualRows.OfType<GridGroup>().Count() == 6);

Console.WriteLine($"  => {passed} passed, {failed} failed");
Environment.ExitCode = failed == 0 ? 0 : 1;

public class Row
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int Salary { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
}
