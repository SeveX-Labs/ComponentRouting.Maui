#if IOS
using CoreGraphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using System;
using System.Linq;
using UIKit;

namespace ComponentRouting.Maui.Routing;

internal sealed class IosOverlayPlatformSurfaceProvider : IOverlayPlatformSurfaceProvider
{
    private readonly IosOverlaySurfaceDiscoveryService discovery;

    public IosOverlayPlatformSurfaceProvider(IosOverlaySurfaceDiscoveryService discovery)
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
        var operationId = OverlayTraceLog.CurrentOperationId ?? "none";
        OverlayTraceLog.Write(
            $"op={operationId} step=ios.provider.surface.requested surface={surfaceKind} provider={OverlayTraceLog.DescribeObject(this)} owner={OverlayTraceLog.DescribeObject(ownerComponent)}");

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
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ios.provider.surface.unavailable surface={surfaceKind} reason=UnsupportedSurface");
        return false;
    }

    private bool TryCreateRootSurfaceCore(Component parentComponent, out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;
        var operationId = OverlayTraceLog.CurrentOperationId ?? "none";
        OverlayTraceLog.Write(
            $"op={operationId} step=ios.provider.root.discovery.begin provider={OverlayTraceLog.DescribeObject(this)} parent={OverlayTraceLog.DescribeObject(parentComponent)}");

        var window = discovery.ResolveRootWindow();
        OverlayTraceLog.Write(
            $"op={operationId} step=ios.provider.root.discovery.end window={DescribeNullableView(window)} rootController={OverlayTraceLog.DescribeObject(window?.RootViewController)}");

        if (window is null)
        {
            OverlayTraceLog.Write(
                $"op={operationId} step=ios.provider.unavailable host=platform-root reason=WindowNull");
            return false;
        }

        return TryCreateSurfaceForParentView(
            "platform-root",
            parentComponent,
            window,
            out surfaceHost);
    }

    private bool TryCreateModalSurface(
        OverlaySurfaceKind surfaceKind,
        Component ownerComponent,
        string hostKind,
        out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;
        var operationId = OverlayTraceLog.CurrentOperationId ?? "none";
        OverlayTraceLog.Write(
            $"op={operationId} step=ios.provider.modal.discovery.begin surface={surfaceKind} owner={OverlayTraceLog.DescribeObject(ownerComponent)}");

        var window = discovery.ResolveRootWindow();
        if (window is null)
        {
            OverlayTraceLog.Write(
                $"op={operationId} step=ios.provider.modal.unavailable surface={surfaceKind} reason=WindowNull");
            return false;
        }

        var candidates = discovery.FindPresentedControllerCandidates(window);
        OverlayTraceLog.Write(
            $"op={operationId} step=ios.provider.modal.discovery.end surface={surfaceKind} window={DescribeView(window)} candidateCount={candidates.Count}");

        foreach (var indexedCandidate in candidates.Select((candidate, index) => new { candidate, index }))
        {
            OverlayTraceLog.Write(
                $"op={operationId} step=ios.provider.modal.candidate index={indexedCandidate.index} depth={indexedCandidate.candidate.Depth} controller={OverlayTraceLog.DescribeObject(indexedCandidate.candidate.Controller)} surfaceController={OverlayTraceLog.DescribeObject(indexedCandidate.candidate.SurfaceController)} view={DescribeNullableView(indexedCandidate.candidate.SurfaceView)}");
        }

        var selected = candidates
            .Select((candidate, index) => new { candidate, index })
            .Where(item => item.candidate.SurfaceView is not null)
            .OrderByDescending(item => item.candidate.Depth)
            .ThenByDescending(item => item.index)
            .FirstOrDefault();

        if (selected?.candidate.SurfaceView is not { } modalView)
        {
            OverlayTraceLog.Write(
                $"op={operationId} step=ios.provider.modal.unavailable surface={surfaceKind} reason=NoPresentedViewCandidate");
            return false;
        }

        OverlayTraceLog.Write(
            $"op={operationId} step=ios.provider.modal.selected surface={surfaceKind} host={hostKind} index={selected.index} depth={selected.candidate.Depth} controller={OverlayTraceLog.DescribeObject(selected.candidate.Controller)} surfaceController={OverlayTraceLog.DescribeObject(selected.candidate.SurfaceController)} view={DescribeView(modalView)}");

        return TryCreateSurfaceForParentView(
            hostKind,
            ownerComponent,
            modalView,
            out surfaceHost);
    }

    private static bool TryCreateSurfaceForParentView(
        string hostKind,
        Component parentComponent,
        UIView parentView,
        out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;
        var operationId = OverlayTraceLog.CurrentOperationId ?? "none";
        var mauiContext = Microsoft.Maui.Controls.Application.Current?
            .Windows
            .FirstOrDefault()?
            .Page?
            .Handler?
            .MauiContext;

        if (mauiContext is null)
        {
            OverlayTraceLog.Write(
                $"op={operationId} step=ios.provider.unavailable host={hostKind} reason=MauiContextNull application={OverlayTraceLog.DescribeObject(Microsoft.Maui.Controls.Application.Current)}");
            return false;
        }

        OverlayTraceLog.Write(
            $"op={operationId} step=ios.provider.mauiContext host={hostKind} context={OverlayTraceLog.DescribeObject(mauiContext)} services={OverlayTraceLog.DescribeObject(mauiContext.Services)} parent={DescribeView(parentView)}");

        UIView? mountedView = null;
        UIView? mountedContainer = null;
        surfaceHost = OverlaySurfaceHost.CreatePlatform(
            parentComponent,
            hostKind,
            layout => mountedView is not null &&
                      mountedContainer is not null &&
                      ReferenceEquals(layout.Handler?.PlatformView, mountedView) &&
                      mountedView.Superview == mountedContainer &&
                      mountedContainer.Superview == parentView,
            layout =>
            {
                var mountOperationId = OverlayTraceLog.CurrentOperationId ?? operationId;
                UIView? nativeView = null;
                UIView? overlayContainer = null;

                try
                {
                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.begin host={hostKind} layout={OverlayTraceLog.DescribeObject(layout)} layoutParent={OverlayTraceLog.DescribeObject(layout.Parent)} layoutHandlerBefore={OverlayTraceLog.DescribeObject(layout.Handler)} parentBefore={DescribeView(parentView)} parentSubviewCountBefore={parentView.Subviews.Length}");

                    nativeView = layout.ToPlatform(mauiContext);
                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.toPlatform host={hostKind} native={DescribeNullableView(nativeView)} handler={OverlayTraceLog.DescribeObject(layout.Handler)} nativeParentBefore={OverlayTraceLog.DescribeObject(nativeView.Superview)}");

                    overlayContainer = CreateOverlayContainer(parentView);
                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.container.created host={hostKind} container={DescribeView(overlayContainer)} userInteraction={overlayContainer.UserInteractionEnabled}");
                    mountedView = nativeView;
                    mountedContainer = overlayContainer;

                    nativeView.RemoveFromSuperview();
                    parentView.AddSubview(overlayContainer);
                    parentView.BringSubviewToFront(overlayContainer);
                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.container.added host={hostKind} container={DescribeView(overlayContainer)} containerParent={OverlayTraceLog.DescribeObject(overlayContainer.Superview)} parentSubviewCountAfter={parentView.Subviews.Length}");

                    nativeView.Frame = overlayContainer.Bounds;
                    nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth |
                                                  UIViewAutoresizing.FlexibleHeight;
                    overlayContainer.AddSubview(nativeView);
                    overlayContainer.BringSubviewToFront(nativeView);
                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.child.added host={hostKind} child={DescribeView(nativeView)} childParent={OverlayTraceLog.DescribeObject(nativeView.Superview)} containerSubviewCount={overlayContainer.Subviews.Length}");

                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.beforeLayout parent={DescribeView(parentView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
                    ForceLayoutPass(parentView, overlayContainer, nativeView);
                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.afterLayout parent={DescribeView(parentView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
                    VerifyMounted(parentView, overlayContainer, nativeView);
                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.success host={hostKind} containerAttached={overlayContainer.Superview == parentView} childAttached={nativeView.Superview == overlayContainer} dimensionsValid={HasValidBounds(overlayContainer) && HasValidBounds(nativeView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");

                    return new OverlaySurfaceHandle(() =>
                    {
                        OverlayTraceLog.Write(
                            $"op={mountOperationId} step=ios.unmount.begin host={hostKind} container={DescribeNullableView(overlayContainer)} child={DescribeNullableView(nativeView)} parentSubviewCountBefore={parentView.Subviews.Length}");
                        Cleanup(layout, nativeView, overlayContainer);
                        OverlayTraceLog.Write(
                            $"op={mountOperationId} step=ios.unmount.end host={hostKind} container={DescribeNullableView(overlayContainer)} child={DescribeNullableView(nativeView)} parentSubviewCountAfter={parentView.Subviews.Length}");
                        mountedContainer = null;
                        mountedView = null;
                    }, hostKind, mountOperationId);
                }
                catch (Exception ex)
                {
                    OverlayTraceLog.Write(
                        $"op={mountOperationId} step=ios.mount.fail host={hostKind} reason=Exception exceptionType={ex.GetType().FullName} message={ex.Message} container={DescribeNullableView(overlayContainer)} child={DescribeNullableView(nativeView)} fallback=legacy");
                    Cleanup(layout, nativeView, overlayContainer);
                    mountedContainer = null;
                    mountedView = null;
                    throw;
                }
            });

        return true;
    }

    private static UIView CreateOverlayContainer(UIView parentView)
    {
        return new UIView(parentView.Bounds)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth |
                               UIViewAutoresizing.FlexibleHeight,
            BackgroundColor = UIColor.Clear,
            Hidden = false,
            Alpha = 1,
            UserInteractionEnabled = true
        };
    }

    private static void ForceLayoutPass(UIView parentView, UIView overlayContainer, UIView nativeView)
    {
        overlayContainer.Frame = parentView.Bounds;
        nativeView.Frame = overlayContainer.Bounds;
        overlayContainer.SetNeedsLayout();
        nativeView.SetNeedsLayout();
        parentView.SetNeedsLayout();
        overlayContainer.LayoutIfNeeded();
        nativeView.LayoutIfNeeded();
        parentView.LayoutIfNeeded();
    }

    private static void VerifyMounted(UIView parentView, UIView overlayContainer, UIView nativeView)
    {
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ios.verify.begin parent={DescribeView(parentView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");

        if (overlayContainer.Superview != parentView)
            throw new InvalidOperationException("iOS overlay container was not attached to the parent view.");

        if (nativeView.Superview != overlayContainer)
            throw new InvalidOperationException("iOS overlay native view was not attached to the overlay container.");

        if (overlayContainer.Hidden || nativeView.Hidden)
            throw new InvalidOperationException(
                $"iOS overlay is hidden. container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");

        if (overlayContainer.Alpha <= 0 || nativeView.Alpha <= 0)
            throw new InvalidOperationException(
                $"iOS overlay is transparent. container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");

        if (!HasValidBounds(parentView))
            throw new InvalidOperationException($"iOS overlay parent has invalid bounds: {DescribeView(parentView)}");

        if (!HasValidBounds(overlayContainer))
            throw new InvalidOperationException($"iOS overlay container has invalid bounds: {DescribeView(overlayContainer)}");

        if (!HasValidBounds(nativeView))
            throw new InvalidOperationException($"iOS overlay child has invalid bounds: {DescribeView(nativeView)}");

        if (parentView.Window is null)
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ios.verify.warning reason=ParentWindowNull parent={DescribeView(parentView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");

        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ios.verify.success parent={DescribeView(parentView)} container={DescribeView(overlayContainer)} child={DescribeView(nativeView)}");
    }

    private static void Cleanup(Layout layout, UIView? nativeView, UIView? overlayContainer)
    {
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ios.cleanup.begin layout={OverlayTraceLog.DescribeObject(layout)} handler={OverlayTraceLog.DescribeObject(layout.Handler)} child={DescribeNullableView(nativeView)} container={DescribeNullableView(overlayContainer)}");

        nativeView?.RemoveFromSuperview();
        overlayContainer?.RemoveFromSuperview();
        layout.Handler?.DisconnectHandler();

        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ios.cleanup.end layout={OverlayTraceLog.DescribeObject(layout)} handler={OverlayTraceLog.DescribeObject(layout.Handler)} child={DescribeNullableView(nativeView)} container={DescribeNullableView(overlayContainer)}");
    }

    private static bool HasValidBounds(UIView view)
    {
        return view.Bounds.Width > 0 && view.Bounds.Height > 0;
    }

    private static string DescribeView(UIView view)
    {
        return $"{OverlayTraceLog.DescribeObject(view)} " +
               $"window={OverlayTraceLog.DescribeObject(view.Window)} superview={OverlayTraceLog.DescribeObject(view.Superview)} " +
               $"hidden={view.Hidden} alpha={view.Alpha} frame={DescribeRect(view.Frame)} bounds={DescribeRect(view.Bounds)} subviews={view.Subviews.Length}";
    }

    private static string DescribeNullableView(UIView? view)
    {
        return view is null ? "null" : DescribeView(view);
    }

    private static string DescribeRect(CGRect rect)
    {
        return $"x={rect.X} y={rect.Y} width={rect.Width} height={rect.Height}";
    }
}
#endif
