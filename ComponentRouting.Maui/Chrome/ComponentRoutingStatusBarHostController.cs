#if IOS
using UIKit;

namespace ComponentRouting.Maui.Chrome;

internal sealed class ComponentRoutingStatusBarHostController : UIViewController
{
    private readonly UIViewController child;
    private readonly IosStatusBarStyleCoordinator coordinator;
    private readonly UIWindow window;

    public ComponentRoutingStatusBarHostController(
        UIViewController child,
        UIWindow window,
        IosStatusBarStyleCoordinator coordinator)
    {
        this.child = child;
        this.window = window;
        this.coordinator = coordinator;
    }

    public override UIStatusBarStyle PreferredStatusBarStyle()
    {
        return coordinator.Resolve(window) ?? child.PreferredStatusBarStyle();
    }

    public override UIViewController? ChildViewControllerForStatusBarStyle()
    {
        return coordinator.Resolve(window) is null ? child : null;
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        AddChildViewController(child);
        View!.AddSubview(child.View!);
        child.View!.Frame = View.Bounds;
        child.View.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
        child.DidMoveToParentViewController(this);
    }
}
#endif
