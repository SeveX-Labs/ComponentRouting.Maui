#if ANDROID
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using System;
using System.Linq;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace ComponentRouting.Maui.Routing;

internal sealed class AndroidRootOverlayPlatformSurfaceProvider : IOverlayPlatformSurfaceProvider
{
    private const string LogTag = "ComponentRouting.Overlay";

    public bool TryCreateRootSurface(Component parentComponent, out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;

        if (Platform.CurrentActivity?.Window?.DecorView is not AViewGroup decorView)
        {
            WriteDebug("Root surface unavailable: current activity decor view is not a ViewGroup.");
            return false;
        }

        var mauiContext = Microsoft.Maui.Controls.Application.Current?
            .Windows
            .FirstOrDefault()?
            .Page?
            .Handler?
            .MauiContext;
        if (mauiContext is null)
        {
            WriteDebug("Root surface unavailable: MauiContext is null.");
            return false;
        }

        AView? mountedView = null;
        FrameLayout? mountedContainer = null;
        surfaceHost = OverlaySurfaceHost.CreatePlatform(
            parentComponent,
            layout => ReferenceEquals(layout.Handler?.PlatformView, mountedView) ||
                      mountedView?.Parent == mountedContainer ||
                      mountedContainer?.Parent == decorView,
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
                        oldParent.RemoveView(nativeView);

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
                    WriteDebug(
                        $"Root surface mounted: decor={DescribeView(decorView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}.");

                    return new OverlaySurfaceHandle(() =>
                    {
                        Cleanup(layout, nativeView, overlayContainer);
                        mountedContainer = null;
                        mountedView = null;
                    });
                }
                catch (Exception ex)
                {
                    WriteDebug($"Root surface mount failed: {ex}");
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
                $"Android root overlay is not shown. container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");

        if (overlayContainer.Width <= 0 || overlayContainer.Height <= 0)
            throw new InvalidOperationException($"Android root overlay container has invalid bounds: {DescribeView(overlayContainer)}");

        if (nativeView.Width <= 0 || nativeView.Height <= 0)
            throw new InvalidOperationException($"Android root overlay child has invalid bounds: {DescribeView(nativeView)}");
    }

    private static void Cleanup(Layout layout, AView? nativeView, FrameLayout? overlayContainer)
    {
        if (nativeView?.Parent is AViewGroup nativeParent)
            nativeParent.RemoveView(nativeView);

        if (overlayContainer?.Parent is AViewGroup containerParent)
            containerParent.RemoveView(overlayContainer);

        layout.Handler?.DisconnectHandler();
        WriteDebug("Root surface cleanup completed.");
    }

    private static string DescribeView(AView view)
    {
        return $"{view.GetType().Name}#{view.GetHashCode():X8} " +
               $"attached={view.Parent is not null} shown={view.IsShown} " +
               $"width={view.Width} height={view.Height} measuredWidth={view.MeasuredWidth} measuredHeight={view.MeasuredHeight}";
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private static void WriteDebug(string message)
    {
        Log.Debug(LogTag, $"[ComponentRouting][OverlaySurface] {message}");
    }
}
#endif
