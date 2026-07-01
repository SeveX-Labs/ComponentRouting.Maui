using ComponentRouting.Maui.Routing;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterRuntimeComponentRegistryTests
{
    [Fact]
    public void IsTracked_returns_false_when_component_is_not_tracked()
    {
        var registry = new RouterRuntimeComponentRegistry();
        var component = new TestComponent();

        Assert.False(registry.IsTracked(component));
    }

    [Fact]
    public void IsTracked_returns_true_after_track()
    {
        var registry = new RouterRuntimeComponentRegistry();
        var component = new TestComponent();

        registry.Track(component);

        Assert.True(registry.IsTracked(component));
    }

    [Fact]
    public void IsTracked_returns_false_after_untrack()
    {
        var registry = new RouterRuntimeComponentRegistry();
        var component = new TestComponent();

        registry.Track(component);
        registry.Untrack(component);

        Assert.False(registry.IsTracked(component));
    }

    [Fact]
    public void IsTracked_uses_reference_identity()
    {
        var registry = new RouterRuntimeComponentRegistry();
        var tracked = new TestComponent();
        var other = new TestComponent();

        registry.Track(tracked);

        Assert.True(registry.IsTracked(tracked));
        Assert.False(registry.IsTracked(other));
    }

    [Fact]
    public void DisposeTrackedComponents_untracks_tracked_components()
    {
        var registry = new RouterRuntimeComponentRegistry();
        var component = new TestComponent();

        registry.Track(component);
        Assert.True(registry.IsTracked(component));

        registry.DisposeTrackedComponents();

        Assert.False(registry.IsTracked(component));
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public void DisposeTrackedComponents_is_idempotent()
    {
        var registry = new RouterRuntimeComponentRegistry();
        var component = new TestComponent();

        registry.Track(component);

        registry.DisposeTrackedComponents();
        registry.DisposeTrackedComponents();

        Assert.False(registry.IsTracked(component));
        Assert.Equal(0, registry.Count);
    }
}
