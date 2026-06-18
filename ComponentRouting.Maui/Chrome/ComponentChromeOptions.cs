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

    public Color? WindowBackgroundColor { get; init; }
    public bool? EdgeToEdge { get; init; }
    public bool? DecorFitsSystemWindows { get; init; }

    public ComponentDisplayCutoutMode? DisplayCutoutMode { get; init; }

    public bool? StatusBarContrastEnforced { get; init; }
    public bool? NavigationBarContrastEnforced { get; init; }

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
            WindowBackgroundColor = higherPriority.WindowBackgroundColor ?? WindowBackgroundColor,
            EdgeToEdge = higherPriority.EdgeToEdge ?? EdgeToEdge,
            DecorFitsSystemWindows = higherPriority.DecorFitsSystemWindows ?? DecorFitsSystemWindows,
            DisplayCutoutMode = higherPriority.DisplayCutoutMode ?? DisplayCutoutMode,
            StatusBarContrastEnforced = higherPriority.StatusBarContrastEnforced ?? StatusBarContrastEnforced,
            NavigationBarContrastEnforced = higherPriority.NavigationBarContrastEnforced ?? NavigationBarContrastEnforced
        };
    }
}
