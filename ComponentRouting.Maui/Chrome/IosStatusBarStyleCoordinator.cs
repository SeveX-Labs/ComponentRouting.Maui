#if IOS
using System.Runtime.CompilerServices;
using UIKit;

namespace ComponentRouting.Maui.Chrome;

internal sealed class IosStatusBarStyleCoordinator
{
    private readonly ConditionalWeakTable<UIWindow, WindowStatusBarState> states = new();

    public void Apply(UIWindow? window, ChromeForeground? foreground)
    {
        if (window is null)
            return;

        states.GetOrCreateValue(window).OverrideStyle = foreground switch
        {
            ChromeForeground.LightContent => UIStatusBarStyle.LightContent,
            ChromeForeground.DarkContent => UIStatusBarStyle.DarkContent,
            _ => null
        };
    }

    public UIStatusBarStyle? Resolve(UIWindow? window)
    {
        return window is not null && states.TryGetValue(window, out var state)
            ? state.OverrideStyle
            : null;
    }

    private sealed class WindowStatusBarState
    {
        public UIStatusBarStyle? OverrideStyle { get; set; }
    }
}
#endif
