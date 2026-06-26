using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ComponentRouting.Maui.Routing;

internal sealed class RouterRuntimeComponentRegistry
{
    private readonly HashSet<Component> components = new(ReferenceEqualityComparer<Component>.Instance);

    public int Count => components.Count;

    public void Track(Component component)
    {
        components.Add(component);
    }

    public void Untrack(Component component)
    {
        components.Remove(component);
    }

    public void DisposeTrackedComponents()
    {
        var componentsSnapshot = components.ToList();

        try
        {
            foreach (var component in componentsSnapshot)
            {
                try
                {
                    component.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
        finally
        {
            components.Clear();
        }
    }
}
