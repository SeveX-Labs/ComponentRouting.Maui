using ComponentRouting.Maui.Routing;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class ComponentHistoryTests
{
    [Fact]
    public void TryGetLatestSnackbar_returns_newest_matching_item()
    {
        var history = new ComponentHistory();
        var older = new CountingComponent();
        var newer = new CountingComponent();

        history.AddSnackbar(typeof(HostComponent), newer, new DateTime(2026, 1, 2));
        history.AddSnackbar(typeof(HostComponent), older, new DateTime(2026, 1, 1));

        var item = history.TryGetLatestSnackbar(typeof(HostComponent));

        Assert.NotNull(item);
        Assert.Same(newer, item.Component);
    }

    [Fact]
    public void TryGetLatestPopup_returns_newest_matching_item()
    {
        var history = new ComponentHistory();
        var older = new CountingComponent();
        var newer = new CountingComponent();

        history.AddPopup(typeof(HostComponent), newer, new DateTime(2026, 1, 2));
        history.AddPopup(typeof(HostComponent), older, new DateTime(2026, 1, 1));

        var item = history.TryGetLatestPopup(typeof(HostComponent));

        Assert.NotNull(item);
        Assert.Same(newer, item.Component);
    }

    [Fact]
    public void DismissMostRecent_unpresents_newest_item()
    {
        var history = new ComponentHistory();
        var olderSnackbar = new CountingComponent();
        var newerPopup = new CountingComponent();
        var snackbarItem = history.AddSnackbar(typeof(HostComponent), olderSnackbar, new DateTime(2026, 1, 1));
        var popupItem = history.AddPopup(typeof(HostComponent), newerPopup, new DateTime(2026, 1, 2));

        history.DismissMostRecent(snackbarItem, popupItem);

        Assert.Equal(0, olderSnackbar.UnpresentCount);
        Assert.Equal(1, newerPopup.UnpresentCount);
    }

    [Fact]
    public void GetMountedComponents_returns_popup_and_snackbar_instances_deduplicated_by_reference()
    {
        var history = new ComponentHistory();
        var popup = new TestComponent();
        var snackbar = new DerivedTestComponent();

        history.AddPopup(typeof(HostComponent), popup, DateTime.Now);
        history.AddSnackbar(typeof(HostComponent), snackbar, DateTime.Now);
        history.AddPopup(typeof(HostComponent), snackbar, DateTime.Now);

        var mounted = history.GetMountedOverlayComponents<Component>();

        Assert.Equal(2, mounted.Count);
        Assert.Contains(popup, mounted);
        Assert.Contains(snackbar, mounted);
    }

    [Fact]
    public void GetMountedComponent_returns_null_when_no_match_exists()
    {
        var history = new ComponentHistory();

        Assert.Null(history.GetMountedOverlayComponent<TestComponent>());
    }

    [Fact]
    public void GetMountedComponent_returns_single_match()
    {
        var history = new ComponentHistory();
        var popup = new TestComponent();
        history.AddPopup(typeof(HostComponent), popup, DateTime.Now);

        Assert.Same(popup, history.GetMountedOverlayComponent<TestComponent>());
    }

    [Fact]
    public void GetMountedOverlayComponent_throws_when_multiple_matches_exist_and_strict_lookup_is_requested()
    {
        var history = new ComponentHistory();
        history.AddPopup(typeof(HostComponent), new TestComponent(), DateTime.Now);
        history.AddPopup(typeof(HostComponent), new TestComponent(), DateTime.Now);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            history.GetMountedOverlayComponent<TestComponent>(throwIfMultiple: true));

        Assert.Contains(nameof(Router.GetMountedOverlayComponents), ex.Message);
    }

    [Fact]
    public void CloseAllPopups_does_not_fail_when_no_popups_are_mounted()
    {
        var history = new ComponentHistory();

        history.CloseAllPopups();

        Assert.Empty(history.Popups);
    }

    [Fact]
    public void CloseAllPopups_unpresents_single_popup_and_removes_it_from_history()
    {
        var history = new ComponentHistory();
        var popup = new CountingComponent();
        history.AddPopup(typeof(HostComponent), popup, DateTime.Now);

        history.CloseAllPopups();

        Assert.Equal(1, popup.UnpresentCount);
        Assert.Empty(history.Popups);
    }

    [Fact]
    public void CloseAllPopups_unpresents_multiple_popups_in_lifo_order()
    {
        var history = new ComponentHistory();
        var closeOrder = new List<string>();
        var first = new OrderedCountingComponent("first", closeOrder);
        var second = new OrderedCountingComponent("second", closeOrder);
        var third = new OrderedCountingComponent("third", closeOrder);

        history.AddPopup(typeof(HostComponent), first, new DateTime(2026, 1, 1));
        history.AddPopup(typeof(HostComponent), second, new DateTime(2026, 1, 2));
        history.AddPopup(typeof(HostComponent), third, new DateTime(2026, 1, 3));

        history.CloseAllPopups();

        Assert.Equal(new[] { "third", "second", "first" }, closeOrder);
        Assert.Equal(1, first.UnpresentCount);
        Assert.Equal(1, second.UnpresentCount);
        Assert.Equal(1, third.UnpresentCount);
        Assert.Empty(history.Popups);
    }

    [Fact]
    public void CloseAllPopups_leaves_snackbars_mounted()
    {
        var history = new ComponentHistory();
        var popup = new CountingComponent();
        var snackbar = new CountingComponent();
        history.AddPopup(typeof(HostComponent), popup, DateTime.Now);
        history.AddSnackbar(typeof(HostComponent), snackbar, DateTime.Now);

        history.CloseAllPopups();

        Assert.Equal(1, popup.UnpresentCount);
        Assert.Equal(0, snackbar.UnpresentCount);
        Assert.Empty(history.Popups);
        var item = Assert.Single(history.Snackbars);
        Assert.Same(snackbar, item.Component);
    }

    [Fact]
    public void Remove_deletes_matching_history_item_by_reference()
    {
        var history = new ComponentHistory();
        var first = new TestComponent();
        var second = new TestComponent();
        history.AddPopup(typeof(HostComponent), first, DateTime.Now);
        history.AddPopup(typeof(HostComponent), second, DateTime.Now);

        Assert.True(history.Remove(second));

        var remaining = Assert.Single(history.Popups);
        Assert.Same(first, remaining.Component);
    }

    [Fact]
    public void GetResumeComponents_dedupes_components_by_reference()
    {
        var component = new TestComponent();

        var components = ComponentHistory.GetResumeComponents(component, component, component, component);

        var mounted = Assert.Single(components);
        Assert.Same(component, mounted);
    }
}
