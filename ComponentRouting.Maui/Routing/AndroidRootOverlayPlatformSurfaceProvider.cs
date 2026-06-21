#if ANDROID
using Android.OS;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using System.Linq;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace ComponentRouting.Maui.Routing;

internal sealed class AndroidRootOverlayPlatformSurfaceProvider : IOverlayPlatformSurfaceProvider
{
    public bool TryCreateRootSurface(Component parentComponent, out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;

        if (Platform.CurrentActivity?.Window?.DecorView is not AViewGroup decorView)
            return false;

        var mauiContext = Microsoft.Maui.Controls.Application.Current?
            .Windows
            .FirstOrDefault()?
            .Page?
            .Handler?
            .MauiContext;
        if (mauiContext is null)
            return false;

        AView? mountedView = null;
        surfaceHost = OverlaySurfaceHost.CreatePlatform(
            parentComponent,
            layout => ReferenceEquals(layout.Handler?.PlatformView, mountedView) ||
                      mountedView?.Parent == decorView,
            layout =>
            {
                var nativeView = layout.ToPlatform(mauiContext);
                mountedView = nativeView;

                if (nativeView.Parent is AViewGroup oldParent)
                    oldParent.RemoveView(nativeView);

                var layoutParams = new FrameLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.MatchParent);

                decorView.AddView(nativeView, layoutParams);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    nativeView.Elevation = 10000f;

                nativeView.BringToFront();
                decorView.Invalidate();

                return new OverlaySurfaceHandle(() =>
                {
                    if (nativeView.Parent is AViewGroup parent)
                        parent.RemoveView(nativeView);

                    layout.Handler?.DisconnectHandler();
                    mountedView = null;
                });
            });

        return true;
    }
}
#endif
