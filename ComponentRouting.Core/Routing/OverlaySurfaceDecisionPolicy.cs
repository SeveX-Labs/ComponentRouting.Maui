namespace ComponentRouting.Maui.Routing;

internal static class OverlaySurfaceDecisionPolicy
{
    public static bool CanUseRootPlatformSurface(OverlaySurfaceKind ownerSurfaceKind, bool hasPopupOwner)
    {
        return ownerSurfaceKind == OverlaySurfaceKind.Root && !hasPopupOwner;
    }
}
