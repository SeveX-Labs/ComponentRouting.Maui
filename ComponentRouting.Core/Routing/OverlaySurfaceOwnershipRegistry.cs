using System.Collections.Generic;

namespace ComponentRouting.Maui.Routing;

internal sealed class OverlaySurfaceOwnershipRegistry
{
    private readonly Dictionary<Component, OverlaySurfaceKind> surfaces = new(ReferenceEqualityComparer.Instance);

    public void Set(Component component, OverlaySurfaceKind surfaceKind)
    {
        surfaces[component] = surfaceKind;
    }

    public bool TryGet(Component component, out OverlaySurfaceKind surfaceKind)
    {
        return surfaces.TryGetValue(component, out surfaceKind);
    }

    public OverlaySurfaceKind GetInheritedSurface(Component? ownerComponent)
    {
        return ownerComponent is not null && TryGet(ownerComponent, out var surfaceKind)
            ? surfaceKind
            : OverlaySurfaceKind.Root;
    }

    public void Remove(Component component)
    {
        surfaces.Remove(component);
    }

    public void Clear()
    {
        surfaces.Clear();
    }
}
