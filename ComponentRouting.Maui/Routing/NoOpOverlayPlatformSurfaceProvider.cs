namespace ComponentRouting.Maui.Routing;

internal sealed class NoOpOverlayPlatformSurfaceProvider : IOverlayPlatformSurfaceProvider
{
    public bool TryCreateRootSurface(Component parentComponent, out OverlaySurfaceHost surfaceHost)
    {
        surfaceHost = null!;
        return false;
    }
}
