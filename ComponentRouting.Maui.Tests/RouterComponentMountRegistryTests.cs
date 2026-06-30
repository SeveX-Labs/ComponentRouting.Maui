using ComponentRouting.Maui.Routing;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterComponentMountRegistryTests
{
    [Fact]
    public void TryResolve_returns_exactly_tracked_mount()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        registry.Track(component, mount);

        Assert.True(registry.TryResolve(component, out var trackedComponent, out var trackedMount));
        Assert.Same(component, trackedComponent);
        Assert.Same(mount, trackedMount);
    }

    [Fact]
    public void TryResolve_returns_latest_component_for_requested_component_type()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var trackedComponent = new TransientLookupComponent();
        var requestedComponent = new TransientLookupComponent();
        var mount = new object();

        registry.Track(trackedComponent, mount);

        Assert.True(registry.TryResolve(requestedComponent, out var resolvedComponent, out var trackedMount));
        Assert.Same(trackedComponent, resolvedComponent);
        Assert.Same(mount, trackedMount);
    }

    [Fact]
    public void TryResolve_returns_tracked_mount_with_owner()
    {
        var registry = new RouterComponentMountRegistry<RouterComponentMount<object, object>>();
        var component = new TestComponent();
        var page = new object();
        var ownerNavigation = new object();
        var mount = new RouterComponentMount<object, object>(page, ownerNavigation);

        registry.Track(component, mount);

        Assert.True(registry.TryResolve(component, out var trackedComponent, out var trackedMount));
        Assert.Same(component, trackedComponent);
        Assert.Same(page, trackedMount.Mount);
        Assert.Same(ownerNavigation, trackedMount.Owner);
    }

    [Fact]
    public void TryBeginFinalize_is_idempotent_until_component_is_tracked_again()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        Assert.True(registry.TryBeginFinalize(component));
        Assert.False(registry.TryBeginFinalize(component));

        registry.Track(component, mount);

        Assert.True(registry.TryBeginFinalize(component));
    }

    [Fact]
    public void Remove_deletes_mount_for_component()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        registry.Track(component, mount);
        registry.Remove(component);

        Assert.False(registry.TryResolve(component, out _, out _));
    }

    [Fact]
    public void ClearMounts_keeps_finalize_state()
    {
        var registry = new RouterComponentMountRegistry<object>();
        var component = new TestComponent();
        var mount = new object();

        registry.Track(component, mount);
        Assert.True(registry.TryBeginFinalize(component));

        registry.ClearMounts();

        Assert.False(registry.TryResolve(component, out _, out _));
        Assert.False(registry.TryBeginFinalize(component));
    }

    private sealed class TransientLookupComponent : TestComponent
    {
    }
}
