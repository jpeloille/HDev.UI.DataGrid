using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Julien.Avalonia.DataGrid.Controls;
using Julien.Avalonia.DataGrid.Models;

// Drives the grid through REAL simulated input (mouse/keyboard via the headless
// input pipeline) and asserts the resulting state — testing the event wiring
// (sort on header click, selection, keyboard nav, filter typing), not direct
// method calls. Saves PNGs of key states.

var outDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "out");
Directory.CreateDirectory(outDir);

int failed = 0;
void Check(string name, bool ok)
{
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {name}");
    if (!ok) failed++;
}

AppBuilder.Configure<CaptureApp>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

var data = new List<Emp>
{
    new() { Name = "Alice", Department = "Engineering", Salary = 92000 },
    new() { Name = "Bob", Department = "Engineering", Salary = 85000 },
    new() { Name = "Carol", Department = "Sales", Salary = 70000 },
    new() { Name = "Dan", Department = "Sales", Salary = 72000 },
    new() { Name = "Eve", Department = "HR", Salary = 65000 },
};

var grid = new JDataGrid { AutoGenerateColumns = false, ItemsSource = data, ShowFilterRow = true };
grid.Columns.Add(new GridColumn { FieldName = nameof(Emp.Name), Header = "Name", Width = new GridLength(2, GridUnitType.Star) });
grid.Columns.Add(new GridColumn { FieldName = nameof(Emp.Department), Header = "Department", Width = new GridLength(1.5, GridUnitType.Star) });
grid.Columns.Add(new GridColumn { FieldName = nameof(Emp.Salary), Header = "Salary", Width = new GridLength(140), ColumnType = ColumnType.Numeric, FormatString = "C0" });
grid.Columns.UpdateVisibleIndices();

var window = new Window { Width = 800, Height = 400, Content = grid };
window.Show();
Pump();

void Pump()
{
    for (int i = 0; i < 3; i++)
    {
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(window.Width, window.Height));
        window.Arrange(new Rect(0, 0, window.Width, window.Height));
    }
    Dispatcher.UIThread.RunJobs();
}

void Save(string name)
{
    window.CaptureRenderedFrame()?.Save(Path.Combine(outDir, name + ".png"), PngBitmapEncoderOptions.Default);
}

void Click(Control c)
{
    var p = c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), window) ?? new Point();
    window.MouseMove(p, RawInputModifiers.None);
    window.MouseDown(p, MouseButton.Left, RawInputModifiers.None);
    window.MouseUp(p, MouseButton.Left, RawInputModifiers.None);
    Pump();
}

JDataGridColumnHeader Header(string field) =>
    grid.GetVisualDescendants().OfType<JDataGridColumnHeader>().First(h => h.Column?.FieldName == field);

List<JDataGridRow> Rows() => grid.GetVisualDescendants().OfType<JDataGridRow>()
    .OrderBy(r => r.TranslatePoint(new Point(), window)?.Y ?? 0).ToList();

// === 1. Sort by clicking the Salary header ===
Click(Header(nameof(Emp.Salary)));
var sorted = grid.View.Cast<Emp>().Select(e => e.Salary).ToList();
Check($"sort: header click sorts ascending ([{string.Join(",", sorted)}])",
    sorted.SequenceEqual(sorted.OrderBy(x => x)));
Check("sort: SortDescriptor recorded for Salary",
    grid.DataSource.SortDescriptors.Any(s => s.FieldName == nameof(Emp.Salary)));
Save("int-01-sorted-asc");

// === 2. Click again -> descending ===
Click(Header(nameof(Emp.Salary)));
var sortedDesc = grid.View.Cast<Emp>().Select(e => e.Salary).ToList();
Check($"sort: second click sorts descending ([{string.Join(",", sortedDesc)}])",
    sortedDesc.SequenceEqual(sortedDesc.OrderByDescending(x => x)));

// === 3. Row selection by click ===
var rows = Rows();
var targetRow = rows.Count > 1 ? rows[1] : rows[0];
var targetItem = targetRow.DataContext as Emp;
Click(targetRow);
Check($"select: clicking a row selects its item (selected='{(grid.SelectedItem as Emp)?.Name}', expected='{targetItem?.Name}')",
    ReferenceEquals(grid.SelectedItem, targetItem));
Save("int-02-selected");

// === 4. Keyboard navigation (Down arrow moves selection) ===
grid.Focus();
Pump();
var beforeNav = grid.SelectedItem as Emp;
window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null);
Pump();
var afterNav = grid.SelectedItem as Emp;
Check($"keyboard: Down arrow moves selection ('{beforeNav?.Name}' -> '{afterNav?.Name}')",
    afterNav != null && !ReferenceEquals(afterNav, beforeNav));

// === 5. Filter typing reduces the view ===
var deptFilter = grid.GetVisualDescendants().OfType<JDataGridFilterCell>()
    .First(f => f.Column?.FieldName == nameof(Emp.Department));
var filterBox = deptFilter.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
if (filterBox != null)
{
    int before = grid.View.Count;
    filterBox.Focus();
    Pump();
    // Type char by char (each keystroke triggers a filter/refresh) to catch the
    // case where the filter cell is recreated and loses text/focus mid-typing.
    foreach (var ch in "Sales")
    {
        window.KeyTextInput(ch.ToString());
        Pump();
    }
    int after = grid.View.Count;
    Check($"filter: typing 'Sales' filters the view ({before} -> {after} rows)", after == 2);
    Check($"filter: textbox retains text after filtering (text='{filterBox.Text}')", filterBox.Text == "Sales");
    Save("int-03-filtered");
}
else
{
    Check("filter: filter textbox found", false);
}

// === 6. Edit via double-click ===
void DoubleClick(Control c)
{
    var p = c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), window) ?? new Point();
    window.MouseMove(p, RawInputModifiers.None);
    window.MouseDown(p, MouseButton.Left, RawInputModifiers.None);
    window.MouseUp(p, MouseButton.Left, RawInputModifiers.None);
    window.MouseDown(p, MouseButton.Left, RawInputModifiers.None);
    window.MouseUp(p, MouseButton.Left, RawInputModifiers.None);
    Pump();
}

var danRow = Rows().FirstOrDefault(r => (r.DataContext as Emp)?.Name == "Dan");
if (danRow != null)
{
    var nameCell = danRow.GetVisualDescendants().OfType<JDataGridCell>().First();
    DoubleClick(nameCell);
    bool editing = grid.GetVisualDescendants().OfType<JDataGridCell>().Any(c => c.IsEditing);
    Check("edit: double-click starts editing", editing);
    if (editing)
    {
        window.KeyTextInput("Daniel");
        Pump();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Pump();
        Check($"edit: commit writes value (Name='{(danRow.DataContext as Emp)?.Name}')",
            (danRow.DataContext as Emp)?.Name == "Daniel");
    }
}

// === 7. Column resize by dragging the header grip ===
void Drag(Point from, Point to)
{
    window.MouseMove(from, RawInputModifiers.None);
    window.MouseDown(from, MouseButton.Left, RawInputModifiers.None);
    window.MouseMove(new Point((from.X + to.X) / 2, from.Y), RawInputModifiers.None);
    window.MouseMove(to, RawInputModifiers.None);
    window.MouseUp(to, MouseButton.Left, RawInputModifiers.None);
    Pump();
}

var salaryHeader = Header(nameof(Emp.Salary));
// The 6px resize grip sits inside the header's 8px right padding, so target
// ~11px in from the right edge, not the very edge (which is padding).
var gripPt = salaryHeader.TranslatePoint(new Point(salaryHeader.Bounds.Width - 11, salaryHeader.Bounds.Height / 2), window) ?? new Point();
double wBefore = grid.Columns.First(c => c.FieldName == nameof(Emp.Salary)).ActualWidth;
Drag(gripPt, new Point(gripPt.X + 70, gripPt.Y));
double wAfter = grid.Columns.First(c => c.FieldName == nameof(Emp.Salary)).ActualWidth;
Check($"resize: dragging the grip widens the column ({wBefore:F0} -> {wAfter:F0})", wAfter > wBefore + 20);
Save("int-04-resized");

// === 8. Column reorder (INFO only) ===
// Reorder uses DragDrop.DoDragDrop (OS-level drag/drop), which the headless
// simulated mouse moves cannot initiate. Not a product verdict — needs the real
// app or a DnD-aware harness. Reported as info, not counted as a failure.
var nameH = Header(nameof(Emp.Name));
var deptH = Header(nameof(Emp.Department));
var fromPt = nameH.TranslatePoint(new Point(nameH.Bounds.Width / 2, nameH.Bounds.Height / 2), window) ?? new Point();
var toPt = deptH.TranslatePoint(new Point(deptH.Bounds.Width / 2, deptH.Bounds.Height / 2), window) ?? new Point();
int idxBefore = grid.Columns.First(c => c.FieldName == nameof(Emp.Name)).VisibleIndex;
Drag(fromPt, toPt);
int idxAfter = grid.Columns.First(c => c.FieldName == nameof(Emp.Name)).VisibleIndex;
Console.WriteLine($"  [INFO] reorder via simulated DnD not exercised (OS drag/drop); Name index {idxBefore} -> {idxAfter} — verify in the real app");

Console.WriteLine(failed == 0 ? "Interaction: ALL PASS" : $"Interaction: {failed} FAILED");
Environment.ExitCode = failed == 0 ? 0 : 1;

public class CaptureApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Interaction"))
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
}
