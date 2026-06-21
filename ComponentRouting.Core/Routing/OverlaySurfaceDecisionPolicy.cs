namespace ComponentRouting.Maui.Routing;

internal static class OverlaySurfaceDecisionPolicy
{
    public static bool CanUsePlatformSurface(
        OverlaySurfaceKind ownerSurfaceKind,
        bool hasPopupOwner,
        bool hasActiveNativeModal)
    {
        var canUsePlatform = ownerSurfaceKind switch
        {
            OverlaySurfaceKind.Root => !hasPopupOwner && !hasActiveNativeModal,
            OverlaySurfaceKind.Modal => !hasPopupOwner && hasActiveNativeModal,
            OverlaySurfaceKind.FullscreenModal => !hasPopupOwner && hasActiveNativeModal,
            _ => false
        };

        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=decision.policy ownerSurface={ownerSurfaceKind} hasPopupOwner={hasPopupOwner} hasActiveNativeModal={hasActiveNativeModal} canUsePlatform={canUsePlatform} reason={GetReason(ownerSurfaceKind, hasPopupOwner, hasActiveNativeModal)}");
        return canUsePlatform;
    }

    private static string GetReason(
        OverlaySurfaceKind ownerSurfaceKind,
        bool hasPopupOwner,
        bool hasActiveNativeModal)
    {
        if (hasPopupOwner)
            return "DeniedPopupOwner";

        return ownerSurfaceKind switch
        {
            OverlaySurfaceKind.Root => hasActiveNativeModal
                ? "DeniedActiveNativeModal"
                : "AllowedRootNoPopupNoModal",
            OverlaySurfaceKind.Modal => hasActiveNativeModal
                ? "AllowedModalActiveNativeModal"
                : "DeniedMissingActiveNativeModal",
            OverlaySurfaceKind.FullscreenModal => hasActiveNativeModal
                ? "AllowedFullscreenModalActiveNativeModal"
                : "DeniedMissingActiveNativeModal",
            _ => "DeniedUnknownSurface"
        };
    }
}
