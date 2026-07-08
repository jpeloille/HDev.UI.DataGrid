using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
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
