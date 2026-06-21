#if IOS
using System.Collections.Generic;
using UIKit;

namespace ComponentRouting.Maui.Routing;

internal sealed class IosOverlaySurfaceDiscoveryService
{
    public UIWindow? ResolveRootWindow()
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

    public IReadOnlyList<IosPresentedControllerCandidate> FindPresentedControllerCandidates(UIWindow window)
    {
        var candidates = new List<IosPresentedControllerCandidate>();
        var depth = 0;

        for (var controller = window.RootViewController?.PresentedViewController;
             controller is not null;
             controller = controller.PresentedViewController)
        {
            var surfaceController = ResolveSurfaceController(controller);
            candidates.Add(new IosPresentedControllerCandidate(
                controller,
                surfaceController,
                surfaceController?.View,
                depth));
            depth++;
        }

        return candidates;
    }

    private static UIViewController? ResolveSurfaceController(UIViewController controller)
    {
        if (controller is UINavigationController navigationController)
            return navigationController.VisibleViewController ?? navigationController.TopViewController ?? navigationController;

        return controller;
    }

    internal sealed record IosPresentedControllerCandidate(
        UIViewController Controller,
        UIViewController? SurfaceController,
        UIView? SurfaceView,
        int Depth);
}
#endif
