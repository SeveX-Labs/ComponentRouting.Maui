using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterHistoryCharacterizationTests
{
    [Fact]
    public void Try_get_latest_snackbar_returns_newest_history_item()
    {
        var router = new TestRouter();
        var older = new CountingSnackbarComponent();
        var newer = new CountingSnackbarComponent();
        var parentType = typeof(HostComponent);

        RouterTestHelpers.AddHistoryItem(router, "Snackbars", newer, parentType, new DateTime(2026, 1, 2));
        RouterTestHelpers.AddHistoryItem(router, "Snackbars", older, parentType, new DateTime(2026, 1, 1));

        var item = RouterTestHelpers.InvokePrivate(router, "TryAndGetLatestHistorySnackbar", parentType)!;
        var component = RouterTestHelpers.GetHistoryItemComponent<CountingSnackbarComponent>(item);

        Assert.Same(newer, component);
    }

    [Fact]
    public void Try_get_latest_popup_returns_newest_history_item()
    {
        var router = new TestRouter();
        var older = new CountingOverlayComponent();
        var newer = new CountingOverlayComponent();
        var parentType = typeof(HostComponent);

        RouterTestHelpers.AddHistoryItem(router, "Popups", newer, parentType, new DateTime(2026, 1, 2));
        RouterTestHelpers.AddHistoryItem(router, "Popups", older, parentType, new DateTime(2026, 1, 1));

        var item = RouterTestHelpers.InvokePrivate(router, "TryAndGetLatestHistoryPopup", parentType)!;
        var component = RouterTestHelpers.GetHistoryItemComponent<CountingOverlayComponent>(item);

        Assert.Same(newer, component);
    }

    [Fact]
    public void Dismiss_most_recent_history_item_unpresents_newest_item()
    {
        var router = new TestRouter();
        var olderSnackbar = new CountingSnackbarComponent();
        var newerPopup = new CountingOverlayComponent();
        var parentType = typeof(HostComponent);
        var snackbarItem = RouterTestHelpers.AddHistoryItem(
            router,
            "Snackbars",
            olderSnackbar,
            parentType,
            new DateTime(2026, 1, 1));
        var popupItem = RouterTestHelpers.AddHistoryItem(
            router,
            "Popups",
            newerPopup,
            parentType,
            new DateTime(2026, 1, 2));

        RouterTestHelpers.InvokePrivate(router, "DismissMostRecentHistoryItem", snackbarItem, popupItem, null);

        Assert.Equal(0, olderSnackbar.UnpresentCount);
        Assert.Equal(1, newerPopup.UnpresentCount);
    }
}
