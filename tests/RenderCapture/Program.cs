using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Julien.Avalonia.DataGrid.Controls;
using Julien.Avalonia.DataGrid.Models;

// Renders the JDataGrid to PNG files using the headless Skia backend, so the
// actual visual output (colors, spacing, alignment) can be inspected without a
// display. Each scenario is saved to tests/RenderCapture/out/.

var outDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "out");
Directory.CreateDirectory(outDir);

AppBuilder.Configure<CaptureApp>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

var data = SampleData();

// Scenario 1: plain grid with explicit columns, formats, summary footer.
Capture("01-basic", BuildGrid(data, group: false, filter: false, summary: true));

// Scenario 2: grouped grid with group summaries.
Capture("02-grouped", BuildGrid(data, group: true, filter: false, summary: true));

// Scenario 3: filter row visible.
Capture("03-filter-row", BuildGrid(data, group: false, filter: true, summary: false));

Console.WriteLine($"Saved PNGs to {Path.GetFullPath(outDir)}");

void Capture(string name, JDataGrid grid)
{
    var window = new Window
    {
        Width = 1100,
        Height = 520,
        Content = grid
    };
    window.Show();
    for (int i = 0; i < 5; i++)
    {
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(window.Width, window.Height));
        window.Arrange(new Rect(0, 0, window.Width, window.Height));
    }
    Dispatcher.UIThread.RunJobs();

    var frame = window.CaptureRenderedFrame();
    var path = Path.Combine(outDir, name + ".png");
    frame?.Save(path);

    var widthDump = string.Join(", ", grid.Columns.Select(c => $"{c.Header}={c.ActualWidth:F0}"));
    Console.WriteLine($"  {name}: {(frame != null ? "ok" : "NULL FRAME")} | widths: {widthDump} | sum={grid.Columns.Sum(c => c.ActualWidth):F0}");
    window.Close();
}

JDataGrid BuildGrid(List<Emp> rows, bool group, bool filter, bool summary)
{
    var grid = new JDataGrid
    {
        AutoGenerateColumns = false,
        ItemsSource = rows,
        ShowFilterRow = filter,
        ShowGroupPanel = group,
        ShowSummaryFooter = summary,
        ShowRowNumbers = true,
    };

    grid.Columns.Add(new GridColumn { FieldName = nameof(Emp.Name), Header = "Name", Width = new GridLength(2, GridUnitType.Star) });
    grid.Columns.Add(new GridColumn { FieldName = nameof(Emp.Department), Header = "Department", Width = new GridLength(1.5, GridUnitType.Star) });
    grid.Columns.Add(new GridColumn { FieldName = nameof(Emp.Salary), Header = "Salary", Width = new GridLength(120), ColumnType = ColumnType.Numeric, FormatString = "C0", TextAlignment = global::Avalonia.Media.TextAlignment.Right });
    grid.Columns.Add(new GridColumn { FieldName = nameof(Emp.Hired), Header = "Hired", Width = new GridLength(110), ColumnType = ColumnType.DateTime, FormatString = "d" });
    grid.Columns.Add(new GridColumn { FieldName = nameof(Emp.Active), Header = "Active", Width = new GridLength(70), ColumnType = ColumnType.Boolean });
    grid.Columns.UpdateVisibleIndices();

    grid.TotalSummaries.Add(new GridSummary { FieldName = nameof(Emp.Salary), SummaryType = SummaryType.Sum, FormatString = "C0", Caption = "Σ {0}" });
    grid.TotalSummaries.Add(new GridSummary { FieldName = nameof(Emp.Name), SummaryType = SummaryType.Count, Caption = "{0} rows" });

    if (group)
    {
        grid.GroupSummaries.Add(new GridSummary { FieldName = nameof(Emp.Salary), SummaryType = SummaryType.Sum, FormatString = "C0", Caption = "Σ {0}" });
        grid.GroupBy(nameof(Emp.Department));
    }

    return grid;
}

List<Emp> SampleData() => new()
{
    new() { Name = "Alice Martin", Department = "Engineering", Salary = 92000, Hired = new DateTime(2018, 3, 1), Active = true },
    new() { Name = "Bob Chen", Department = "Engineering", Salary = 85000, Hired = new DateTime(2019, 7, 12), Active = false },
    new() { Name = "Carol Diaz", Department = "Sales", Salary = 70000, Hired = new DateTime(2020, 1, 5), Active = true },
    new() { Name = "Dan Okoro", Department = "Sales", Salary = 72000, Hired = new DateTime(2021, 11, 20), Active = true },
    new() { Name = "Eve Wilson", Department = "HR", Salary = 65000, Hired = new DateTime(2017, 9, 9), Active = false },
    new() { Name = "Frank Li", Department = "Engineering", Salary = 98000, Hired = new DateTime(2016, 5, 23), Active = true },
    new() { Name = "Grace Park", Department = "Sales", Salary = 68000, Hired = new DateTime(2022, 2, 14), Active = true },
};

public class CaptureApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://RenderCapture"))
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
