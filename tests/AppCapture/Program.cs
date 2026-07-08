using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DemoApp.ViewModels;
using DemoApp.Views;

// Renders the full DemoApp MainWindow (ribbon, card panels, skins) to PNG via
// the headless Skia backend, so the app chrome can be inspected without a
// display. Output goes to tests/AppCapture/out/.

var outDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "out");
Directory.CreateDirectory(outDir);

AppBuilder.Configure<DemoApp.App>()
    .UseSkia()
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
    .SetupWithoutStarting();

var window = new MainWindow();
var vm = (MainViewModel)window.DataContext!;
window.Show();

Capture("app-blue-default");

foreach (var skin in vm.AvailableSkins.Skip(1))
{
    vm.SelectedSkin = skin;
    Capture($"app-{skin.Name.ToLowerInvariant()}");
}

vm.SelectedSkin = vm.AvailableSkins[0];
vm.SelectedEmployee = vm.Employees[2];
vm.ShowFilterRow = true;
vm.ShowGroupPanel = true;
Capture("app-blue-panels-selection");

// Search box with text + focus: verifies the filled/active display (text
// rendered via the two-way SearchText binding, watermark gone, focus accent
// border).
var searchCard = window.GetVisualDescendants()
    .OfType<DemoApp.Controls.CardPanel>()
    .FirstOrDefault(c => c.ShowSearch);
var searchBox = searchCard?.GetVisualDescendants()
    .OfType<TextBox>()
    .FirstOrDefault(t => t.Name == "PART_SearchBox");
int totalBefore = vm.Employees.Count;
if (searchBox != null)
{
    // Type into the box (accent-less on purpose: search must match "Valérie").
    searchBox.Focus();
    searchBox.Text = "valerie";
}
Capture("app-blue-search-active");
bool filtered = vm.Employees.Count > 0 && vm.Employees.Count < totalBefore
    && vm.Employees.All(emp => emp.FirstName.Contains("Valérie") || emp.LastName.Contains("Valérie"));
Console.WriteLine($"  search end-to-end: box='{searchBox?.Text}' -> vm.SearchText='{vm.SearchText}', rows {totalBefore} -> {vm.Employees.Count}, status='{vm.StatusText}' | {(filtered ? "FILTERED OK" : "NOT FILTERED")}");

window.Close();
Console.WriteLine($"Saved PNGs to {Path.GetFullPath(outDir)}");

void Capture(string name)
{
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
    Console.WriteLine($"  {name}: {(frame != null ? "ok" : "NULL FRAME")}");
}
