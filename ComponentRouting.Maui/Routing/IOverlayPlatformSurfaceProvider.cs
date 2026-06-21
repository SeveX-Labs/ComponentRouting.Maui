namespace ComponentRouting.Maui.Routing;

internal interface IOverlayPlatformSurfaceProvider
{
    bool TryCreateRootSurface(Component parentComponent, out OverlaySurfaceHost surfaceHost);

    bool TryCreateSurface(
        OverlaySurfaceKind surfaceKind,
        Component ownerComponent,
        out OverlaySurfaceHost surfaceHost)
    {
        if (surfaceKind == OverlaySurfaceKind.Root)
            return TryCreateRootSurface(ownerComponent, out surfaceHost);

        surfaceHost = null!;
        return false;
    }
}
