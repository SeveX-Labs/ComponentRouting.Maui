using System;
using System.Collections.Generic;

namespace ComponentRouting.Maui.Routing;

internal sealed class RouterComponentMountRegistry<TMount>
    where TMount : class
{
    private readonly Dictionary<Component, TMount> mountsByComponent =
        new(ReferenceEqualityComparer<Component>.Instance);

    private readonly Dictionary<Type, Component> latestComponentByType = new();

    private readonly HashSet<Component> finalizedComponents =
        new(ReferenceEqualityComparer<Component>.Instance);

    public void Track(Component component, TMount mount)
    {
        mountsByComponent[component] = mount;
        latestComponentByType[component.GetType()] = component;
        finalizedComponents.Remove(component);
    }

    public bool TryResolve(Component requestedComponent, out Component trackedComponent, out TMount mount)
    {
        if (mountsByComponent.TryGetValue(requestedComponent, out mount!))
        {
            trackedComponent = requestedComponent;
            return true;
        }

        if (latestComponentByType.TryGetValue(requestedComponent.GetType(), out trackedComponent!) &&
            mountsByComponent.TryGetValue(trackedComponent, out mount!))
        {
            return true;
        }

        trackedComponent = null!;
        mount = null!;
        return false;
    }

    public void Remove(Component component)
    {
        mountsByComponent.Remove(component);

        if (latestComponentByType.TryGetValue(component.GetType(), out var latestComponent) &&
            ReferenceEquals(latestComponent, component))
        {
            latestComponentByType.Remove(component.GetType());
        }
    }

    public bool TryBeginFinalize(Component component)
    {
        return finalizedComponents.Add(component);
    }

    public void ClearMounts()
    {
        mountsByComponent.Clear();
        latestComponentByType.Clear();
    }

    public void Clear()
    {
        ClearMounts();
        finalizedComponents.Clear();
    }
}
