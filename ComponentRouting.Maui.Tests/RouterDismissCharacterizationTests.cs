using ComponentRouting.Maui.Exceptions;
using Xunit;

namespace ComponentRouting.Maui.Tests;

[Collection(MauiApplicationCollection.Name)]
public class RouterDismissCharacterizationTests
{
    [Fact]
    public async Task DismissComponent_unsupported_component_throws_router_exception()
    {
        MauiApplicationTestHost.EnsureApplication();
        var component = new UnsupportedRoutableComponent();
        var router = new ConfigurableTestRouter(new InstanceComponentFactory(component));

        var exception = await Assert.ThrowsAsync<RouterException>(
            () => router.DismissComponent<UnsupportedRoutableComponent, object, object>());

        Assert.Equal("ComponentDismissalNotSupported", exception.Error.ToString());
        Assert.Same(component, exception.Component);
        Assert.Empty(router.ComponentsStack);
    }
}
