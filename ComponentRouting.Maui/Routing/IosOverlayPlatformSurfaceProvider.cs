#if IOS
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

        var window = discovery.ResolveRootWindow();

        if (window is null)
        {
            return false;
        }

        if (!TryResolveRootParentView(window, out var parentView, out var parentKind))
        {
            return false;
        }

        return TryCreateSurfaceForParentView(
            "platform-root",
            parentComponent,
            window,
            parentView,
            parentKind,
            out surfaceHost);
    }

    private bool TryCreateModalSurface(
        OverlaySurfaceKind surfaceKind,
        Component ownerComponent,
        string hostKind,
        out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;

        var window = discovery.ResolveRootWindow();
        if (window is null)
        {
            return false;
        }

        var candidates = discovery.FindPresentedControllerCandidates(window);

        var selected = candidates
            .Select((candidate, index) => new { candidate, index })
            .OrderByDescending(item => item.candidate.Depth)
            .ThenByDescending(item => item.index)
            .FirstOrDefault(item => TryResolveModalParentView(item.candidate, out _, out _));

        if (selected is null ||
            !TryResolveModalParentView(selected.candidate, out var modalView, out var parentKind))
        {
            return false;
        }

        return TryCreateSurfaceForParentView(
            hostKind,
            ownerComponent,
            window,
            modalView,
            parentKind,
            out surfaceHost);
    }

    private static bool TryCreateSurfaceForParentView(
        string hostKind,
        Component parentComponent,
        UIWindow window,
        UIView parentView,
        string parentKind,
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
                UIView? nativeView = null;
                UIView? overlayContainer = null;

                try
                {
                    nativeView = layout.ToPlatform(mauiContext);

                    overlayContainer = CreateOverlayContainer(parentView);
                    mountedView = nativeView;
                    mountedContainer = overlayContainer;

                    nativeView.RemoveFromSuperview();
                    parentView.AddSubview(overlayContainer);
                    parentView.BringSubviewToFront(overlayContainer);

                    nativeView.Frame = overlayContainer.Bounds;
                    nativeView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth |
                                                  UIViewAutoresizing.FlexibleHeight;
                    overlayContainer.AddSubview(nativeView);
                    overlayContainer.BringSubviewToFront(nativeView);

                    RequestLayoutPass(parentView, overlayContainer, nativeView);
                    VerifyMounted(parentView, overlayContainer, nativeView);
                    overlayContainer.UserInteractionEnabled = true;

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

    private static UIView CreateOverlayContainer(UIView parentView)
    {
        return new UIView(parentView.Bounds)
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth |
                               UIViewAutoresizing.FlexibleHeight,
            BackgroundColor = UIColor.Clear,
            Hidden = false,
            Alpha = 1,
            UserInteractionEnabled = false
        };
    }

    private static void RequestLayoutPass(UIView parentView, UIView overlayContainer, UIView nativeView)
    {
        overlayContainer.Frame = parentView.Bounds;
        nativeView.Frame = overlayContainer.Bounds;
        overlayContainer.SetNeedsLayout();
        nativeView.SetNeedsLayout();
        parentView.SetNeedsLayout();
    }

    private static void VerifyMounted(UIView parentView, UIView overlayContainer, UIView nativeView)
    {
        if (overlayContainer.Superview != parentView)
            throw new InvalidOperationException("iOS overlay container was not attached to the parent view.");

        if (nativeView.Superview != overlayContainer)
            throw new InvalidOperationException("iOS overlay native view was not attached to the overlay container.");

        if (overlayContainer.Hidden || nativeView.Hidden)
            throw new InvalidOperationException("iOS overlay is hidden.");

        if (overlayContainer.Alpha <= 0 || nativeView.Alpha <= 0)
            throw new InvalidOperationException("iOS overlay is transparent.");

        if (!HasValidBounds(parentView))
            throw new InvalidOperationException("iOS overlay parent has invalid bounds.");

        if (!HasValidBounds(overlayContainer))
            throw new InvalidOperationException("iOS overlay container has invalid bounds.");

        if (!HasValidBounds(nativeView))
            throw new InvalidOperationException("iOS overlay child has invalid bounds.");

        if (parentView.Window is null && parentView is not UIWindow)
            throw new InvalidOperationException("iOS overlay parent is not attached to a window.");
    }

    private static bool TryResolveRootParentView(
        UIWindow window,
        out UIView parentView,
        out string parentKind)
    {
        var rootController = window.RootViewController;
        var rootView = rootController?.IsViewLoaded == true
            ? rootController.View
            : null;

        if (rootView is not null &&
            rootView.Window is not null &&
            HasValidBounds(rootView) &&
            !rootView.Hidden &&
            rootView.Alpha > 0)
        {
            parentView = rootView;
            parentKind = "RootControllerView";
            return true;
        }

        if (HasValidBounds(window) &&
            !window.Hidden &&
            window.Alpha > 0)
        {
            parentView = window;
            parentKind = "Window";
            return true;
        }

        parentView = null!;
        parentKind = "None";
        return false;
    }

    private static bool TryResolveModalParentView(
        IosOverlaySurfaceDiscoveryService.IosPresentedControllerCandidate candidate,
        out UIView parentView,
        out string parentKind)
    {
        if (candidate.Controller is UINavigationController navigationController)
        {
            if (TryResolveValidControllerView(
                    navigationController.VisibleViewController,
                    "NavigationVisibleViewController",
                    out parentView,
                    out parentKind))
            {
                return true;
            }

            if (TryResolveValidControllerView(
                    navigationController.TopViewController,
                    "NavigationTopViewController",
                    out parentView,
                    out parentKind))
            {
                return true;
            }

            if (TryResolveValidControllerView(
                    navigationController,
                    "NavigationControllerView",
                    out parentView,
                    out parentKind))
            {
                return true;
            }
        }

        if (TryResolveValidControllerView(
                candidate.SurfaceController,
                "PresentedSurfaceControllerView",
                out parentView,
                out parentKind))
        {
            return true;
        }

        if (TryResolveValidControllerView(
                candidate.Controller,
                "PresentedControllerView",
                out parentView,
                out parentKind))
        {
            return true;
        }

        parentView = null!;
        parentKind = "None";
        return false;
    }

    private static bool TryResolveValidControllerView(
        UIViewController? controller,
        string candidateKind,
        out UIView parentView,
        out string parentKind)
    {
        if (controller?.IsViewLoaded == true &&
            controller.View is { } view &&
            view.Window is not null &&
            HasValidBounds(view) &&
            !view.Hidden &&
            view.Alpha > 0)
        {
            parentView = view;
            parentKind = candidateKind;
            return true;
        }

        parentView = null!;
        parentKind = "None";
        return false;
    }

    private static void Cleanup(Layout layout, UIView? nativeView, UIView? overlayContainer)
    {
        nativeView?.RemoveFromSuperview();
        overlayContainer?.RemoveFromSuperview();
        layout.Handler?.DisconnectHandler();
    }

    private static bool HasValidBounds(UIView view)
    {
        return view.Bounds.Width > 0 && view.Bounds.Height > 0;
    }

}
#endif
