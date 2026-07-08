using Avalonia;
using Avalonia.Controls;

namespace DemoApp.Controls;

public class RibbonGroup : ContentControl
{
    public static readonly StyledProperty<string?> GroupLabelProperty =
        AvaloniaProperty.Register<RibbonGroup, string?>(nameof(GroupLabel));

    public string? GroupLabel
    {
        get => GetValue(GroupLabelProperty);
        set => SetValue(GroupLabelProperty, value);
    }
}
