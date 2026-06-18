using Microsoft.Maui.Graphics;

namespace ComponentRouting.Maui.Chrome;

public sealed class ComponentChromeOptions
{
    public Color? StatusBarBackgroundColor { get; init; }
    public ChromeForeground? StatusBarForeground { get; init; }

    public Color? NavigationBarBackgroundColor { get; init; }
    public ChromeForeground? NavigationBarForeground { get; init; }

    public Color? ActionBarBackgroundColor { get; init; }
    public Color? ActionBarTextColor { get; init; }

    public bool? EdgeToEdge { get; init; }

    public ComponentChromeOptions Merge(ComponentChromeOptions? higherPriority)
    {
        if (higherPriority is null)
            return this;

        return new ComponentChromeOptions
        {
            StatusBarBackgroundColor = higherPriority.StatusBarBackgroundColor ?? StatusBarBackgroundColor,
            StatusBarForeground = higherPriority.StatusBarForeground ?? StatusBarForeground,
            NavigationBarBackgroundColor = higherPriority.NavigationBarBackgroundColor ?? NavigationBarBackgroundColor,
            NavigationBarForeground = higherPriority.NavigationBarForeground ?? NavigationBarForeground,
            ActionBarBackgroundColor = higherPriority.ActionBarBackgroundColor ?? ActionBarBackgroundColor,
            ActionBarTextColor = higherPriority.ActionBarTextColor ?? ActionBarTextColor,
            EdgeToEdge = higherPriority.EdgeToEdge ?? EdgeToEdge
        };
    }
}
