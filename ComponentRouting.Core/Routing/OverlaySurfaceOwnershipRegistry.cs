using System.Collections.Generic;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceOwnershipRegistry
{
    private readonly Dictionary<Component, OverlaySurfaceKind> surfaces = new(ReferenceEqualityComparer.Instance);

    public void Set(Component component, OverlaySurfaceKind surfaceKind, string? reason = null)
    {
        surfaces[component] = surfaceKind;
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ownership.set reason={reason ?? "Unspecified"} component={OverlayTraceLog.DescribeObject(component)} surface={surfaceKind} count={surfaces.Count}");
    }

    public bool TryGet(Component component, out OverlaySurfaceKind surfaceKind)
    {
        var found = surfaces.TryGetValue(component, out surfaceKind);
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ownership.tryGet component={OverlayTraceLog.DescribeObject(component)} found={found} surface={(found ? surfaceKind : OverlaySurfaceKind.Unknown)} count={surfaces.Count}");
        return found;
    }

    public OverlaySurfaceKind GetInheritedSurface(Component? ownerComponent)
    {
        var inheritedSurface = ownerComponent is not null && TryGet(ownerComponent, out var surfaceKind)
            ? surfaceKind
            : OverlaySurfaceKind.Root;
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ownership.inherit owner={OverlayTraceLog.DescribeObject(ownerComponent)} inheritedSurface={inheritedSurface}");
        return inheritedSurface;
    }

    public void Remove(Component component, string? reason = null)
    {
        var removed = surfaces.Remove(component);
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ownership.remove reason={reason ?? "Unspecified"} component={OverlayTraceLog.DescribeObject(component)} removed={removed} count={surfaces.Count}");
    }

    public void Clear(string? reason = null)
    {
        surfaces.Clear();
        OverlayTraceLog.Write(
            $"op={OverlayTraceLog.CurrentOperationId ?? "none"} step=ownership.clear reason={reason ?? "Unspecified"} count=0");
    }
}
