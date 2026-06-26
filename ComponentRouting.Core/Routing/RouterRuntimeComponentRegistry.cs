using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

    public Task ShutdownTrackedComponentsAsync(RouterShutdownContext context)
    {
        var componentsSnapshot = components.ToList();
        if (!componentsSnapshot.Any())
            return Task.CompletedTask;

        return ShutdownTrackedComponentsInternalAsync(componentsSnapshot, context);
    }

    private async Task ShutdownTrackedComponentsInternalAsync(
        IReadOnlyList<Component> componentsSnapshot,
        RouterShutdownContext context)
    {
        try
        {
            foreach (var component in componentsSnapshot)
            {
                await NotifyComponentShutdownAsync(component, context);
                await NotifyPresenterShutdownAsync(component, context);
            }

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

    private static async ValueTask NotifyComponentShutdownAsync(
        Component component,
        RouterShutdownContext context)
    {
        if (component is not IRouterShutdownAwareComponent shutdownAwareComponent)
            return;

        try
        {
            await shutdownAwareComponent.OnRouterShutdownAsync(context);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private static async ValueTask NotifyPresenterShutdownAsync(
        Component component,
        RouterShutdownContext context)
    {
        if (component.Presenter is not IRouterShutdownAwarePresenter shutdownAwarePresenter)
            return;

        try
        {
            await shutdownAwarePresenter.OnRouterShutdownAsync(context);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}
