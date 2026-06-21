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
        var operationId = OverlayTraceLog.CurrentOperationId ?? "none";
        WriteTrace(
            $"op={operationId} step=android.provider.begin provider={OverlayTraceLog.DescribeObject(this)} parent={OverlayTraceLog.DescribeObject(parentComponent)} activity={OverlayTraceLog.DescribeObject(Platform.CurrentActivity)} window={OverlayTraceLog.DescribeObject(Platform.CurrentActivity?.Window)} decor={OverlayTraceLog.DescribeObject(Platform.CurrentActivity?.Window?.DecorView)}");

        if (Platform.CurrentActivity?.Window?.DecorView is not AViewGroup decorView)
        {
            WriteTrace(
                $"op={operationId} step=android.provider.unavailable reason=DecorViewNotViewGroup decor={OverlayTraceLog.DescribeObject(Platform.CurrentActivity?.Window?.DecorView)}");
            WriteDebug("Root surface unavailable: current activity decor view is not a ViewGroup.");
            return false;
        }

        WriteTrace(
            $"op={operationId} step=android.provider.decor {DescribeView(decorView)} parent={OverlayTraceLog.DescribeObject(decorView.Parent)} childCount={decorView.ChildCount}");

        var mauiContext = Microsoft.Maui.Controls.Application.Current?
            .Windows
            .FirstOrDefault()?
            .Page?
            .Handler?
            .MauiContext;
        if (mauiContext is null)
        {
            WriteTrace(
                $"op={operationId} step=android.provider.unavailable reason=MauiContextNull application={OverlayTraceLog.DescribeObject(Microsoft.Maui.Controls.Application.Current)}");
            WriteDebug("Root surface unavailable: MauiContext is null.");
            return false;
        }
        WriteTrace(
            $"op={operationId} step=android.provider.mauiContext context={OverlayTraceLog.DescribeObject(mauiContext)} services={OverlayTraceLog.DescribeObject(mauiContext.Services)}");

        AView? mountedView = null;
        FrameLayout? mountedContainer = null;
        surfaceHost = OverlaySurfaceHost.CreatePlatform(
            parentComponent,
            layout => mountedView is not null &&
                      mountedContainer is not null &&
                      ReferenceEquals(layout.Handler?.PlatformView, mountedView) &&
                      mountedView.Parent == mountedContainer &&
                      mountedContainer.Parent == decorView,
            layout =>
            {
                var mountOperationId = OverlayTraceLog.CurrentOperationId ?? operationId;
                AView? nativeView = null;
                FrameLayout? overlayContainer = null;

                try
                {
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.begin layout={OverlayTraceLog.DescribeObject(layout)} layoutParent={OverlayTraceLog.DescribeObject(layout.Parent)} layoutHandlerBefore={OverlayTraceLog.DescribeObject(layout.Handler)} decorBefore={DescribeView(decorView)} decorChildCountBefore={decorView.ChildCount}");
                    nativeView = layout.ToPlatform(mauiContext);
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.toPlatform native={DescribeNullableView(nativeView)} handler={OverlayTraceLog.DescribeObject(layout.Handler)} nativeParentBefore={OverlayTraceLog.DescribeObject(nativeView.Parent)}");
                    overlayContainer = CreateOverlayContainer(decorView);
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.container.created container={DescribeView(overlayContainer)} clickable={overlayContainer.Clickable} focusable={overlayContainer.Focusable} layoutParams={DescribeLayoutParams(overlayContainer.LayoutParameters)}");
                    mountedView = nativeView;
                    mountedContainer = overlayContainer;

                    if (nativeView.Parent is AViewGroup oldParent)
                    {
                        WriteTrace(
                            $"op={mountOperationId} step=android.mount.detachOldParent oldParent={OverlayTraceLog.DescribeObject(oldParent)} oldParentChildCountBefore={oldParent.ChildCount}");
                        oldParent.RemoveView(nativeView);
                        WriteTrace(
                            $"op={mountOperationId} step=android.mount.detachOldParent.done oldParentChildCountAfter={oldParent.ChildCount}");
                    }

                    decorView.AddView(
                        overlayContainer,
                        new FrameLayout.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            ViewGroup.LayoutParams.MatchParent));
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.container.added container={DescribeView(overlayContainer)} containerParent={OverlayTraceLog.DescribeObject(overlayContainer.Parent)} decorChildCountAfter={decorView.ChildCount} layoutParams={DescribeLayoutParams(overlayContainer.LayoutParameters)}");
                    overlayContainer.AddView(
                        nativeView,
                        new FrameLayout.LayoutParams(
                            ViewGroup.LayoutParams.MatchParent,
                            ViewGroup.LayoutParams.MatchParent));
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.child.added child={DescribeView(nativeView)} childParent={OverlayTraceLog.DescribeObject(nativeView.Parent)} containerChildCount={overlayContainer.ChildCount} layoutParams={DescribeLayoutParams(nativeView.LayoutParameters)}");

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                    {
                        overlayContainer.Elevation = 10000f;
                        nativeView.Elevation = 10000f;
                    }

                    overlayContainer.BringToFront();
                    nativeView.BringToFront();
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.beforeLayout decor={DescribeView(decorView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
                    ForceLayoutPass(decorView, overlayContainer, nativeView);
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.afterLayout decor={DescribeView(decorView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
                    VerifyMounted(decorView, overlayContainer, nativeView);
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.success containerAttached={overlayContainer.Parent == decorView} childAttached={nativeView.Parent == overlayContainer} dimensionsValid={overlayContainer.Width > 0 && overlayContainer.Height > 0 && nativeView.Width > 0 && nativeView.Height > 0} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
                    WriteDebug(
                        $"Root surface mounted: decor={DescribeView(decorView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}.");

                    return new OverlaySurfaceHandle(() =>
                    {
                        WriteTrace(
                            $"op={mountOperationId} step=android.unmount.begin container={DescribeNullableView(overlayContainer)} child={DescribeNullableView(nativeView)} decorChildCountBefore={decorView.ChildCount}");
                        Cleanup(layout, nativeView, overlayContainer);
                        WriteTrace(
                            $"op={mountOperationId} step=android.unmount.end container={DescribeNullableView(overlayContainer)} child={DescribeNullableView(nativeView)} decorChildCountAfter={decorView.ChildCount}");
                        mountedContainer = null;
                        mountedView = null;
                    }, "platform-root", mountOperationId);
                }
                catch (Exception ex)
                {
                    WriteTrace(
                        $"op={mountOperationId} step=android.mount.fail reason=Exception exceptionType={ex.GetType().FullName} message={ex.Message} container={DescribeNullableView(overlayContainer)} child={DescribeNullableView(nativeView)} fallback=legacy");
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
        WriteTrace(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.layout.force width={width} height={height} decor={DescribeView(decorView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");

        if (width <= 0 || height <= 0)
        {
            overlayContainer.RequestLayout();
            nativeView.RequestLayout();
            decorView.Invalidate();
            WriteTrace(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.layout.force.requestOnly reason=DecorHasNoBounds decor={DescribeView(decorView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
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
        WriteTrace(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.verify.begin decor={DescribeView(decorView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
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
        WriteTrace(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.verify.success decor={DescribeView(decorView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
    }

    private static void Cleanup(Layout layout, AView? nativeView, FrameLayout? overlayContainer)
    {
        WriteTrace(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.cleanup.begin layout={OverlayTraceLog.DescribeObject(layout)} handler={OverlayTraceLog.DescribeObject(layout.Handler)} child={DescribeNullableView(nativeView)} container={DescribeNullableView(overlayContainer)}");
        if (nativeView?.Parent is AViewGroup nativeParent)
        {
            WriteTrace(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.cleanup.removeChild parent={OverlayTraceLog.DescribeObject(nativeParent)} parentChildCountBefore={nativeParent.ChildCount}");
            nativeParent.RemoveView(nativeView);
            WriteTrace(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.cleanup.removeChild.done parentChildCountAfter={nativeParent.ChildCount}");
        }

        if (overlayContainer?.Parent is AViewGroup containerParent)
        {
            WriteTrace(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.cleanup.removeContainer parent={OverlayTraceLog.DescribeObject(containerParent)} parentChildCountBefore={containerParent.ChildCount}");
            containerParent.RemoveView(overlayContainer);
            WriteTrace(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.cleanup.removeContainer.done parentChildCountAfter={containerParent.ChildCount}");
        }

        layout.Handler?.DisconnectHandler();
        WriteTrace(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=android.cleanup.end layout={OverlayTraceLog.DescribeObject(layout)} handler={OverlayTraceLog.DescribeObject(layout.Handler)} child={DescribeNullableView(nativeView)} container={DescribeNullableView(overlayContainer)}");
        WriteDebug("Root surface cleanup completed.");
    }

    private static string DescribeView(AView view)
    {
        return $"{OverlayTraceLog.DescribeObject(view)} " +
               $"attached={view.Parent is not null} shown={view.IsShown} " +
               $"visibility={view.Visibility} width={view.Width} height={view.Height} measuredWidth={view.MeasuredWidth} measuredHeight={view.MeasuredHeight} elevation={(Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop ? view.Elevation : 0)}";
    }

    private static string DescribeNullableView(AView? view)
    {
        return view is null ? "null" : DescribeView(view);
    }

    private static string DescribeLayoutParams(ViewGroup.LayoutParams? layoutParams)
    {
        return layoutParams is null
            ? "null"
            : $"{layoutParams.GetType().FullName} width={layoutParams.Width} height={layoutParams.Height}";
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private static void WriteDebug(string message)
    {
        Log.Debug(LogTag, $"[ComponentRouting][OverlaySurface] {message}");
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private static void WriteTrace(string message)
    {
        Log.Debug(LogTag, $"[ComponentRouting][OverlayTrace] {message}");
    }
}
#endif
