using ComponentRouting.Maui.Model.Core;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceResolver
{
    public bool TryResolveOverlaySurface(
        Component? latestPopupComponent,
        Component? latestStackComponent,
        Component? mountedComponent,
        bool hasActiveModal,
        IOverlayPlatformSurfaceProvider? platformSurfaceProvider,
        out OverlaySurfaceHost surfaceHost)
    {
        if (TryGetLegacyOverlayHost(latestPopupComponent, out surfaceHost))
            return true;

        var parentComponent = latestStackComponent ?? mountedComponent;
        if (!hasActiveModal &&
            parentComponent is not null &&
            platformSurfaceProvider is not null &&
            platformSurfaceProvider.TryCreateRootSurface(parentComponent, out surfaceHost))
        {
            return true;
        }

        if (TryGetLegacyOverlayHost(latestStackComponent, out surfaceHost))
            return true;

        if (TryGetLegacyOverlayHost(mountedComponent, out surfaceHost))
            return true;

        surfaceHost = null!;
        return false;
    }

    public bool TryResolveLegacyOverlayHost(
        Component? latestPopupComponent,
        Component? latestStackComponent,
        Component? mountedComponent,
        out OverlaySurfaceHost surfaceHost)
    {
        if (TryGetLegacyOverlayHost(latestPopupComponent, out surfaceHost))
            return true;

        if (TryGetLegacyOverlayHost(latestStackComponent, out surfaceHost))
            return true;

        if (TryGetLegacyOverlayHost(mountedComponent, out surfaceHost))
            return true;

        surfaceHost = null!;
        return false;
    }

    private static bool TryGetLegacyOverlayHost(Component? component, out OverlaySurfaceHost surfaceHost)
    {
        if (component?.Presenter is OverlayHost { OverlayContainer: not null } overlayHost)
        {
            surfaceHost = OverlaySurfaceHost.CreateLegacy(component, overlayHost.OverlayContainer);
            return true;
        }

        surfaceHost = null!;
        return false;
    }
}
