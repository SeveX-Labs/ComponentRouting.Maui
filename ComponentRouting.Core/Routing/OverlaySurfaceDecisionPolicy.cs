namespace ComponentRouting.Maui.Routing;

internal static class OverlaySurfaceDecisionPolicy
{
    public static bool CanUseRootPlatformSurface(OverlaySurfaceKind ownerSurfaceKind)
    {
        return ownerSurfaceKind == OverlaySurfaceKind.Root;
    }
}
