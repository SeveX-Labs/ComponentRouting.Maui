namespace ComponentRouting.Maui.Routing;

internal interface IOverlayPlatformSurfaceProvider
{
    bool TryCreateRootSurface(Component parentComponent, out OverlaySurfaceHost surfaceHost);
}
