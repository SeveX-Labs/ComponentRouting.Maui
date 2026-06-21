#if IOS
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using System.Linq;
using UIKit;

namespace ComponentRouting.Maui.Routing;

internal sealed class IosRootOverlayPlatformSurfaceProvider : IOverlayPlatformSurfaceProvider
{
    public bool TryCreateRootSurface(Component parentComponent, out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;

        var window = ResolveWindow();
        if (window is null)
            return false;

        var mauiContext = Microsoft.Maui.Controls.Application.Current?
            .Windows
            .FirstOrDefault()?
            .Page?
            .Handler?
            .MauiContext;
        if (mauiContext is null)
            return false;

        UIView? mountedView = null;
        surfaceHost = OverlaySurfaceHost.CreatePlatform(
            parentComponent,
            layout => ReferenceEquals(layout.Handler?.PlatformView, mountedView) ||
                      mountedView?.Superview == window,
            layout =>
            {
                var nativeView = layout.ToPlatform(mauiContext);
                mountedView = nativeView;

                nativeView.RemoveFromSuperview();
                nativeView.Frame = window.Bounds;
                nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth |
                                              UIViewAutoresizing.FlexibleHeight;
                window.AddSubview(nativeView);
                window.BringSubviewToFront(nativeView);

                return new OverlaySurfaceHandle(() =>
                {
                    nativeView.RemoveFromSuperview();
                    layout.Handler?.DisconnectHandler();
                    mountedView = null;
                });
            });

        return true;
    }

    private static UIWindow? ResolveWindow()
    {
        UIWindow? fallbackWindow = null;

        foreach (var scene in UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is not UIWindowScene windowScene)
                continue;

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
}
#endif
