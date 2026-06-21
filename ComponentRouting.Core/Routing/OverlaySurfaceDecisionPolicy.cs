namespace ComponentRouting.Maui.Routing;

internal static class OverlaySurfaceDecisionPolicy
{
    public static bool CanUseRootPlatformSurface(
        OverlaySurfaceKind ownerSurfaceKind,
        bool hasPopupOwner,
        bool hasActiveNativeModal)
    {
        var canUsePlatform =
            ownerSurfaceKind == OverlaySurfaceKind.Root &&
            !hasPopupOwner &&
            !hasActiveNativeModal;
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

        if (hasActiveNativeModal)
            return "DeniedActiveNativeModal";

        return ownerSurfaceKind switch
        {
            OverlaySurfaceKind.Root => "AllowedRootNoPopupNoModal",
            OverlaySurfaceKind.Modal => "DeniedModalSurface",
            OverlaySurfaceKind.FullscreenModal => "DeniedFullscreenModalSurface",
            _ => "DeniedUnknownSurface"
        };
    }
}
