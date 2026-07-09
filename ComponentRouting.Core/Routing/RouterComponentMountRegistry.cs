using System;
using System.Collections.Generic;

namespace ComponentRouting.Maui.Routing;

internal sealed class RouterComponentMountRegistry<TMount>
    where TMount : class
{
    private readonly object gate = new();

    private readonly Dictionary<Component, TMount> mountsByComponent =
        new(ReferenceEqualityComparer<Component>.Instance);

    private readonly Dictionary<Type, Component> latestComponentByType = new();

    // Components whose finalization is CURRENTLY in progress: an in-flight guard against concurrent/
    // reentrant FinalizeComponent, NOT a permanent "already finalized" set. Added by TryBeginFinalize and
    // removed by EndFinalize (from FinalizeComponent's finally), so the SAME instance can be finalized again
    // on a later present/close — essential for reused singleton modals (which are never re-Track'd).
    private readonly HashSet<Component> finalizingComponents =
        new(ReferenceEqualityComparer<Component>.Instance);

    public void Track(Component component, TMount mount)
    {
        lock (gate)
        {
            mountsByComponent[component] = mount;
            latestComponentByType[component.GetType()] = component;
            finalizingComponents.Remove(component);
        }
    }

    public bool TryResolve(Component requestedComponent, out Component trackedComponent, out TMount mount)
    {
        lock (gate)
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
    }

    public void Remove(Component component)
    {
        lock (gate)
        {
            mountsByComponent.Remove(component);

            if (latestComponentByType.TryGetValue(component.GetType(), out var latestComponent) &&
                ReferenceEquals(latestComponent, component))
            {
                latestComponentByType.Remove(component.GetType());
            }
        }
    }

    // Returns true if finalization may begin now (this instance is not already being finalized). The caller
    // MUST call EndFinalize when done (see AbstractRouter.FinalizeComponent's finally) so the SAME instance
    // can be finalized again on a later close — this is what makes reused singleton modals dispose every
    // time instead of only once.
    public bool TryBeginFinalize(Component component)
    {
        lock (gate)
            return finalizingComponents.Add(component);
    }

    // Marks the in-flight finalization complete, allowing a future finalization of the same instance.
    public void EndFinalize(Component component)
    {
        lock (gate)
            finalizingComponents.Remove(component);
    }

    public void ClearMounts()
    {
        lock (gate)
        {
            mountsByComponent.Clear();
            latestComponentByType.Clear();
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            mountsByComponent.Clear();
            latestComponentByType.Clear();
            finalizingComponents.Clear();
        }
    }
}
