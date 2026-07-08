using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace DemoApp.Controls;

public class CardPanel : ContentControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<CardPanel, string?>(nameof(Title));

    public static readonly StyledProperty<bool> ShowSearchProperty =
        AvaloniaProperty.Register<CardPanel, bool>(nameof(ShowSearch), false);

    public static readonly StyledProperty<bool> ShowHeaderProperty =
        AvaloniaProperty.Register<CardPanel, bool>(nameof(ShowHeader), true);

    public static readonly StyledProperty<bool> CanCollapseProperty =
        AvaloniaProperty.Register<CardPanel, bool>(nameof(CanCollapse), false);

    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<CardPanel, bool>(nameof(IsCollapsed), false);

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool ShowSearch
    {
        get => GetValue(ShowSearchProperty);
        set => SetValue(ShowSearchProperty, value);
    }

    public bool ShowHeader
    {
        get => GetValue(ShowHeaderProperty);
        set => SetValue(ShowHeaderProperty, value);
    }

    public bool CanCollapse
    {
        get => GetValue(CanCollapseProperty);
        set => SetValue(CanCollapseProperty, value);
    }

    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        var collapseButton = e.NameScope.Find<Button>("PART_CollapseButton");
        if (collapseButton != null)
        {
            collapseButton.Click += (_, _) => IsCollapsed = !IsCollapsed;
        }
    }
}
