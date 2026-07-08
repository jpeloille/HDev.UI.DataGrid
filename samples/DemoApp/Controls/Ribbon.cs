using Avalonia;
using Avalonia.Controls;

namespace DemoApp.Controls;

public class Ribbon : TabControl
{
    public static readonly StyledProperty<bool> IsMinimizedProperty =
        AvaloniaProperty.Register<Ribbon, bool>(nameof(IsMinimized), false);

    public bool IsMinimized
    {
        get => GetValue(IsMinimizedProperty);
        set => SetValue(IsMinimizedProperty, value);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new RibbonTab();
    }

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is RibbonTab)
        {
            recycleKey = null;
            return false;
        }
        recycleKey = null;
        return true;
    }
}
