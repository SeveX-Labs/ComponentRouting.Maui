#if ANDROID
using Android.OS;
using Android.Views;
using Android.Widget;
using ComponentRouting.Maui.Chrome;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using System;
using System.Linq;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace ComponentRouting.Maui.Routing;

internal sealed class AndroidOverlayPlatformSurfaceProvider : IOverlayPlatformSurfaceProvider
{
    private readonly AndroidModalWindowDiscoveryService discovery;

    public AndroidOverlayPlatformSurfaceProvider(AndroidModalWindowDiscoveryService discovery)
    {
        this.discovery = discovery;
    }

    public bool TryCreateRootSurface(Component parentComponent, out OverlaySurfaceHost surfaceHost)
    {
        return TryCreateSurface(OverlaySurfaceKind.Root, parentComponent, out surfaceHost);
    }

    public bool TryCreateSurface(
        OverlaySurfaceKind surfaceKind,
        Component ownerComponent,
        out OverlaySurfaceHost surfaceHost)
    {
        return surfaceKind switch
        {
            OverlaySurfaceKind.Root => TryCreateRootSurfaceCore(ownerComponent, out surfaceHost),
            OverlaySurfaceKind.Modal => TryCreateModalSurface(surfaceKind, ownerComponent, "platform-modal", out surfaceHost),
            OverlaySurfaceKind.FullscreenModal => TryCreateModalSurface(surfaceKind, ownerComponent, "platform-fullscreen-modal", out surfaceHost),
            _ => TryRejectUnknownSurface(surfaceKind, out surfaceHost)
        };
    }

    private static bool TryRejectUnknownSurface(
        OverlaySurfaceKind surfaceKind,
        out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;
        return false;
    }

    private bool TryCreateRootSurfaceCore(Component parentComponent, out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;

        if (Platform.CurrentActivity?.Window?.DecorView is not AViewGroup decorView)
        {
            return false;
        }

        return TryCreateSurfaceForDecorView(
            "platform-root",
            parentComponent,
            decorView,
            out surfaceHost);
    }

    private bool TryCreateModalSurface(
        OverlaySurfaceKind surfaceKind,
        Component ownerComponent,
        string hostKind,
        out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;

        var candidates = discovery.FindModalDialogWindows(ownerComponent);

        var selected = candidates
            .Select((candidate, index) => new { candidate, index })
            .Where(item => item.candidate.DecorView is AViewGroup)
            .OrderByDescending(item => item.candidate.Depth)
            .ThenByDescending(item => item.index)
            .FirstOrDefault();

        if (selected?.candidate.DecorView is not AViewGroup modalDecorView)
        {
            return false;
        }

        return TryCreateSurfaceForDecorView(
            hostKind,
            ownerComponent,
            modalDecorView,
            out surfaceHost);
    }

    private static bool TryCreateSurfaceForDecorView(
        string hostKind,
        Component parentComponent,
        AViewGroup decorView,
        out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;
        var mauiContext = Microsoft.Maui.Controls.Application.Current?
            .Windows
            .FirstOrDefault()?
            .Page?
            .Handler?
            .MauiContext;
        if (mauiContext is null)
        {
            return false;
        }

        AView? mountedView = null;
        FrameLayout? mountedContainer = null;
        surfaceHost = OverlaySurfaceHost.CreatePlatform(
            parentComponent,
            hostKind,
            layout => mountedView is not null &&
                      mountedContainer is not null &&
                      ReferenceEquals(layout.Handler?.PlatformView, mountedView) &&
                      mountedView.Parent == mountedContainer &&
                      mountedContainer.Parent == decorView,
            layout =>
            {
                AView? nativeView = null;
                FrameLayout? overlayContainer = null;

                try
                {
                    nativeView = layout.ToPlatform(mauiContext);
                    overlayContainer = CreateOverlayContainer(decorView);
                    mountedView = nativeView;
                    mountedContainer = overlayContainer;

                    if (nativeView.Parent is AViewGroup oldParent)
                    {
                        oldParent.RemoveView(nativeView);
                    }

                    decorView.AddView(
                        overlayContainer,
                        new FrameLayout.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            ViewGroup.LayoutParams.MatchParent));
                    overlayContainer.AddView(
                        nativeView,
                        new FrameLayout.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            ViewGroup.LayoutParams.MatchParent));

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    {
                        overlayContainer.Elevation = 10000f;
                        nativeView.Elevation = 10000f;
                    }

                    overlayContainer.BringToFront();
                    nativeView.BringToFront();
                    ForceLayoutPass(decorView, overlayContainer, nativeView);
                    VerifyMounted(decorView, overlayContainer, nativeView);

                    return new OverlaySurfaceHandle(() =>
                    {
                        Cleanup(layout, nativeView, overlayContainer);
                        mountedContainer = null;
                        mountedView = null;
                    }, hostKind);
                }
                catch
                {
                    Cleanup(layout, nativeView, overlayContainer);
                    mountedContainer = null;
                    mountedView = null;
                    throw;
                }
            });

        return true;
    }

    private static FrameLayout CreateOverlayContainer(AViewGroup decorView)
    {
        var overlayContainer = new FrameLayout(decorView.Context)
        {
            Clickable = true,
            Focusable = false,
            Visibility = ViewStates.Visible
        };

        overlayContainer.SetClipToPadding(false);
        overlayContainer.SetClipChildren(false);

        return overlayContainer;
    }

    private static void ForceLayoutPass(AViewGroup decorView, FrameLayout overlayContainer, AView nativeView)
    {
        var width = decorView.Width > 0 ? decorView.Width : decorView.MeasuredWidth;
        var height = decorView.Height > 0 ? decorView.Height : decorView.MeasuredHeight;

        if (width <= 0 || height <= 0)
        {
            overlayContainer.RequestLayout();
            nativeView.RequestLayout();
            decorView.Invalidate();
            return;
        }

        var widthSpec = AView.MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly);
        var heightSpec = AView.MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly);

        overlayContainer.Measure(widthSpec, heightSpec);
        overlayContainer.Layout(0, 0, width, height);
        nativeView.Measure(widthSpec, heightSpec);
        nativeView.Layout(0, 0, width, height);

        overlayContainer.RequestLayout();
        nativeView.RequestLayout();
        overlayContainer.Invalidate();
        nativeView.Invalidate();
        decorView.Invalidate();
    }

    private static void VerifyMounted(AViewGroup decorView, FrameLayout overlayContainer, AView nativeView)
    {
        if (overlayContainer.Parent != decorView)
            throw new InvalidOperationException("Android root overlay container was not attached to the decor view.");

        if (nativeView.Parent != overlayContainer)
            throw new InvalidOperationException("Android root overlay native view was not attached to the overlay container.");

        if (!overlayContainer.IsShown || !nativeView.IsShown)
            throw new InvalidOperationException(
                "Android root overlay is not shown.");

        if (overlayContainer.Width <= 0 || overlayContainer.Height <= 0)
            throw new InvalidOperationException("Android root overlay container has invalid bounds.");

        if (nativeView.Width <= 0 || nativeView.Height <= 0)
            throw new InvalidOperationException("Android root overlay child has invalid bounds.");
    }

    private static void Cleanup(Layout layout, AView? nativeView, FrameLayout? overlayContainer)
    {
        if (nativeView?.Parent is AViewGroup nativeParent)
        {
            nativeParent.RemoveView(nativeView);
        }

        if (overlayContainer?.Parent is AViewGroup containerParent)
        {
            containerParent.RemoveView(overlayContainer);
        }

        layout.Handler?.DisconnectHandler();
    }
}
#endif
