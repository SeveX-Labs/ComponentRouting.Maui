#if IOS
using UIKit;

namespace ComponentRouting.Maui.Chrome;

internal sealed class IosStatusBarHostControllerService
{
    private readonly IosStatusBarStyleCoordinator coordinator;

    public IosStatusBarHostControllerService(IosStatusBarStyleCoordinator coordinator)
    {
        this.coordinator = coordinator;
    }

    public UIWindow? ResolveWindow()
    {
        UIWindow? fallbackWindow = null;

        foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is not UIWindowScene windowScene)
                continue;

            if (windowScene.ActivationState is not UISceneActivationState.ForegroundActive
                and not UISceneActivationState.ForegroundInactive)
            {
                continue;
            }

            foreach (var window in windowScene.Windows)
            {
                if (window.IsKeyWindow)
                    return window;

                if (fallbackWindow is null && !window.Hidden)
                    fallbackWindow = window;
            }
        }

        return fallbackWindow;
    }

    public ComponentRoutingStatusBarHostController? EnsureInstalled(UIWindow? window)
    {
        var rootViewController = window?.RootViewController;
        if (window is null || rootViewController is null)
            return null;

        if (rootViewController is ComponentRoutingStatusBarHostController existingHost)
            return existingHost;

        var host = new ComponentRoutingStatusBarHostController(rootViewController, window, coordinator);
        window.RootViewController = host;
        return host;
    }

    public void RequestStatusBarUpdate(UIWindow? window)
    {
        if (window?.RootViewController is ComponentRoutingStatusBarHostController host)
            host.SetNeedsStatusBarAppearanceUpdate();
    }
}
#endif
