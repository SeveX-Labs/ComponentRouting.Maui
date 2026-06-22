namespace ComponentRouting.Maui.Routing;

internal static class OverlaySurfaceDecisionPolicy
{
    public static bool CanUsePlatformSurface(
        OverlaySurfaceKind ownerSurfaceKind,
        bool hasPopupOwner,
        bool hasActiveNativeModal)
    {
        return ownerSurfaceKind switch
        {
            OverlaySurfaceKind.Root => !hasPopupOwner && !hasActiveNativeModal,
            OverlaySurfaceKind.Modal => !hasPopupOwner && hasActiveNativeModal,
            OverlaySurfaceKind.FullscreenModal => !hasPopupOwner && hasActiveNativeModal,
            _ => false
        };
    }
}
