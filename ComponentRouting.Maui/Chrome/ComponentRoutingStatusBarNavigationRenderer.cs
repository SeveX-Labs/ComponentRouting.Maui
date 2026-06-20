#if IOS
using Microsoft.Maui;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using UIKit;

namespace ComponentRouting.Maui.Chrome;

internal sealed class ComponentRoutingStatusBarNavigationRenderer : NavigationRenderer
{
    public override UIStatusBarStyle PreferredStatusBarStyle()
    {
        return ResolveNavigationPage()?.StatusBarForeground switch
        {
            ChromeForeground.LightContent => UIStatusBarStyle.LightContent,
            ChromeForeground.DarkContent => UIStatusBarStyle.DarkContent,
            _ => base.PreferredStatusBarStyle()
        };
    }

    public override UIViewController? ChildViewControllerForStatusBarStyle()
    {
        return ResolveNavigationPage()?.StatusBarForeground is ChromeForeground.LightContent or ChromeForeground.DarkContent
            ? null
            : base.ChildViewControllerForStatusBarStyle();
    }

    private ComponentRoutingStatusBarNavigationPage? ResolveNavigationPage()
    {
        if (Element is ComponentRoutingStatusBarNavigationPage elementPage)
            return elementPage;

        return this is IElementHandler { VirtualView: ComponentRoutingStatusBarNavigationPage virtualPage }
            ? virtualPage
            : null;
    }
}
#endif
