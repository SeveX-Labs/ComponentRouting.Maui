using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceResolver
{
    public bool TryResolveOverlaySurface(
        Component? latestPopupComponent,
        Component? latestStackComponent,
        Component? mountedComponent,
        OverlaySurfaceKind ownerSurfaceKind,
        bool hasActiveNativeModal,
        IOverlayPlatformSurfaceProvider? platformSurfaceProvider,
        out OverlaySurfaceHost surfaceHost)
    {
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.begin ownerSurface={ownerSurfaceKind} hasPopupOwner={latestPopupComponent is not null} hasActiveNativeModal={hasActiveNativeModal} provider={OverlayTraceLog.DescribeObject(platformSurfaceProvider)} latestPopup={OverlayTraceLog.DescribeObject(latestPopupComponent)} latestStack={OverlayTraceLog.DescribeObject(latestStackComponent)} mounted={OverlayTraceLog.DescribeObject(mountedComponent)}");
        if (TryGetLegacyOverlayHost(latestPopupComponent, out surfaceHost))
        {
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.end host=legacy reason=PopupOwner parent={OverlayTraceLog.DescribeObject(surfaceHost.ParentComponent)}");
            return true;
        }

        var parentComponent = latestStackComponent ?? mountedComponent;
        if (OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
                ownerSurfaceKind,
                hasPopupOwner: latestPopupComponent is not null,
                hasActiveNativeModal) &&
            parentComponent is not null &&
            platformSurfaceProvider is not null &&
            platformSurfaceProvider.TryCreateSurface(ownerSurfaceKind, parentComponent, out surfaceHost))
        {
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.end host={surfaceHost.HostKind} reason=ProviderAccepted ownerSurface={ownerSurfaceKind} parent={OverlayTraceLog.DescribeObject(surfaceHost.ParentComponent)}");
            return true;
        }

        if (TryGetLegacyOverlayHost(latestStackComponent, out surfaceHost))
        {
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.end host=legacy reason=LatestStack parent={OverlayTraceLog.DescribeObject(surfaceHost.ParentComponent)}");
            return true;
        }

        if (TryGetLegacyOverlayHost(mountedComponent, out surfaceHost))
        {
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.end host=legacy reason=Mounted parent={OverlayTraceLog.DescribeObject(surfaceHost.ParentComponent)}");
            return true;
        }

        surfaceHost = null!;
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.end host=none reason=NoOverlayHost");
        return false;
    }

    public bool TryResolveLegacyOverlayHost(
        Component? latestPopupComponent,
        Component? latestStackComponent,
        Component? mountedComponent,
        out OverlaySurfaceHost surfaceHost)
    {
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.legacy.begin latestPopup={OverlayTraceLog.DescribeObject(latestPopupComponent)} latestStack={OverlayTraceLog.DescribeObject(latestStackComponent)} mounted={OverlayTraceLog.DescribeObject(mountedComponent)}");
        if (TryGetLegacyOverlayHost(latestPopupComponent, out surfaceHost))
        {
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.legacy.end found=true reason=PopupOwner parent={OverlayTraceLog.DescribeObject(surfaceHost.ParentComponent)}");
            return true;
        }

        if (TryGetLegacyOverlayHost(latestStackComponent, out surfaceHost))
        {
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.legacy.end found=true reason=LatestStack parent={OverlayTraceLog.DescribeObject(surfaceHost.ParentComponent)}");
            return true;
        }

        if (TryGetLegacyOverlayHost(mountedComponent, out surfaceHost))
        {
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.legacy.end found=true reason=Mounted parent={OverlayTraceLog.DescribeObject(surfaceHost.ParentComponent)}");
            return true;
        }

        surfaceHost = null!;
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.legacy.end found=false reason=NoOverlayHost");
        return false;
    }

    private static bool TryGetLegacyOverlayHost(Component? component, out OverlaySurfaceHost surfaceHost)
    {
        if (component?.Presenter is OverlayHost { OverlayContainer: not null } overlayHost)
        {
            surfaceHost = OverlaySurfaceHost.CreateLegacy(component, overlayHost.OverlayContainer);
            OverlayTraceLog.Write(
                $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.legacy.candidate found=true component={OverlayTraceLog.DescribeObject(component)} presenter={OverlayTraceLog.DescribeObject(component.Presenter)} container={OverlayTraceLog.DescribeObject(overlayHost.OverlayContainer)} containerParent={OverlayTraceLog.DescribeObject(overlayHost.OverlayContainer.Parent)}");
            return true;
        }

        surfaceHost = null!;
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=resolver.legacy.candidate found=false component={OverlayTraceLog.DescribeObject(component)} presenter={OverlayTraceLog.DescribeObject(component?.Presenter)}");
        return false;
    }
}
