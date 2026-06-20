using Microsoft.Maui.Controls;

namespace ComponentRouting.Maui.Chrome;

internal sealed class ComponentRoutingStatusBarNavigationPage : NavigationPage
{
    public ComponentRoutingStatusBarNavigationPage(
        Page root,
        ChromeForeground statusBarForeground,
        ComponentPresentationKind presentationKind)
        : base(root)
    {
        StatusBarForeground = statusBarForeground;
        PresentationKind = presentationKind;
    }

    public ChromeForeground StatusBarForeground { get; }
    public ComponentPresentationKind PresentationKind { get; }
}
