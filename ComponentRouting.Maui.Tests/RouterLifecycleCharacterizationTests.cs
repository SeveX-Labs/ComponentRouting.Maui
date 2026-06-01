using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterLifecycleCharacterizationTests
{
    [Fact]
    public void DispatchResume_resumes_same_component_only_once()
    {
        var router = new TestRouter();
        var component = new ResumeCountingComponent();

        RouterTestHelpers.SetRouterProperty(router, "MountedComponent", component);
        RouterTestHelpers.SetRouterProperty(router, "CurrentTabComponent", component);
        RouterTestHelpers.SetRouterProperty(router, "CurrentFlyoutComponent", component);
        router.ComponentsStack.Add(component);

        router.DispatchResume();

        Assert.Equal(1, component.ResumeCount);
    }
}
