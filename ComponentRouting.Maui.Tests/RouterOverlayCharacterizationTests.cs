using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterOverlayCharacterizationTests
{
    [Fact]
    public void PresentOverlayComponent_returns_false_when_presenter_is_null()
    {
        var router = new TestRouter();
        var overlay = new TestOverlayComponent();

        var didPresent = InvokePresentOverlayComponent(router, overlay);

        Assert.False(didPresent);
        Assert.Empty(RouterTestHelpers.GetHistoryComponents(router, "Popups"));
    }

    [Fact]
    public void PresentOverlayComponent_returns_false_when_overlay_host_is_not_available()
    {
        var router = new TestRouter();
        var overlay = new TestOverlayComponent();
        RouterTestHelpers.SetComponentPresenter(overlay, new TestOverlayPresenter());

        var didPresent = InvokePresentOverlayComponent(router, overlay);

        Assert.False(didPresent);
        Assert.Empty(RouterTestHelpers.GetHistoryComponents(router, "Popups"));
    }

    [Fact]
    public void PresentOverlayComponent_returns_false_when_layout_is_already_mounted()
    {
        var router = new TestRouter();
        var parent = new HostComponent();
        var overlay = new TestOverlayComponent();
        var presenter = new TestOverlayPresenter();
        RouterTestHelpers.SetComponentPresenter(overlay, presenter);
        RouterTestHelpers.SetRouterProperty(router, "MountedComponent", parent);
        parent.Host.OverlayContainer!.Children.Add(presenter);

        var didPresent = InvokePresentOverlayComponent(router, overlay);

        Assert.False(didPresent);
        Assert.Single(parent.Host.OverlayContainer.Children);
        Assert.Empty(RouterTestHelpers.GetHistoryComponents(router, "Popups"));
    }

    [Fact]
    public void PresentOverlayComponent_returns_true_when_overlay_is_added_to_host()
    {
        var router = new TestRouter();
        var parent = new HostComponent();
        var overlay = new TestOverlayComponent();
        var presenter = new TestOverlayPresenter();
        RouterTestHelpers.SetComponentPresenter(overlay, presenter);
        RouterTestHelpers.SetRouterProperty(router, "MountedComponent", parent);

        var didPresent = InvokePresentOverlayComponent(router, overlay);

        Assert.True(didPresent);
        Assert.Contains(presenter, parent.Host.OverlayContainer!.Children);
        var history = RouterTestHelpers.GetHistoryComponents(router, "Popups");
        var mounted = Assert.Single(history);
        Assert.Same(overlay, mounted);
    }

    private static bool InvokePresentOverlayComponent(TestRouter router, Component component)
    {
        return (bool)RouterTestHelpers.InvokePrivate(router, "PresentOverlayComponent", component)!;
    }
}
