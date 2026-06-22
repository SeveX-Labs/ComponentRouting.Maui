using ComponentRouting.Maui.Routing;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class OverlaySurfaceRuntimeSafetyTests
{
    [Theory]
    [InlineData((int)OverlaySurfaceKind.Root, false, false, true)]
    [InlineData((int)OverlaySurfaceKind.Root, false, true, false)]
    [InlineData((int)OverlaySurfaceKind.Modal, false, true, true)]
    [InlineData((int)OverlaySurfaceKind.Modal, false, false, false)]
    [InlineData((int)OverlaySurfaceKind.FullscreenModal, false, true, true)]
    [InlineData((int)OverlaySurfaceKind.FullscreenModal, false, false, false)]
    [InlineData((int)OverlaySurfaceKind.Unknown, false, false, false)]
    [InlineData((int)OverlaySurfaceKind.Unknown, false, true, false)]
    public void OverlaySurfaceDecisionPolicy_matches_surface_contract(
        int surfaceKind,
        bool hasPopupOwner,
        bool hasActiveNativeModal,
        bool expected)
    {
        var canUsePlatform = OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            (OverlaySurfaceKind)surfaceKind,
            hasPopupOwner,
            hasActiveNativeModal);

        Assert.Equal(expected, canUsePlatform);
    }

    [Theory]
    [InlineData((int)OverlaySurfaceKind.Root, false)]
    [InlineData((int)OverlaySurfaceKind.Modal, true)]
    [InlineData((int)OverlaySurfaceKind.FullscreenModal, true)]
    [InlineData((int)OverlaySurfaceKind.Unknown, true)]
    public void OverlaySurfaceDecisionPolicy_denies_every_surface_when_popup_owner_exists(
        int surfaceKind,
        bool hasActiveNativeModal)
    {
        var canUsePlatform = OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            (OverlaySurfaceKind)surfaceKind,
            hasPopupOwner: true,
            hasActiveNativeModal);

        Assert.False(canUsePlatform);
    }

    [Theory]
    [InlineData(0, false, true)]
    [InlineData(1, true, false)]
    [InlineData(2, true, false)]
    public void LegacyOverlayContainerInputState_keeps_empty_container_non_interactive(
        int childCount,
        bool expectedIsVisible,
        bool expectedInputTransparent)
    {
        var state = LegacyOverlayContainerInputState.FromChildCount(childCount);

        Assert.Equal(expectedIsVisible, state.IsVisible);
        Assert.Equal(expectedInputTransparent, state.InputTransparent);
    }

    [Fact]
    public void LegacyOverlayContainerInputState_rejects_negative_child_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LegacyOverlayContainerInputState.FromChildCount(-1));
    }

    [Fact]
    public void ComponentHistory_cleans_up_effective_legacy_handle_after_platform_fallback()
    {
        var history = new ComponentHistory();
        var popup = new TestComponent();
        var platformUnmounts = 0;
        var legacyUnmounts = 0;
        _ = new OverlaySurfaceHandle(() => platformUnmounts++, "platform-root");
        var legacyHandle = new OverlaySurfaceHandle(() => legacyUnmounts++, "legacy");

        history.AddPopup(typeof(HostComponent), popup, DateTime.Now, legacyHandle);

        var removed = history.ClearPopups();

        Assert.Same(popup, Assert.Single(removed));
        Assert.Equal(0, platformUnmounts);
        Assert.Equal(1, legacyUnmounts);
        Assert.Empty(history.Popups);
    }

    [Fact]
    public void ComponentHistory_clear_does_not_unmount_non_effective_platform_handle_after_fallback()
    {
        var history = new ComponentHistory();
        var popup = new TestComponent();
        var platformUnmounts = 0;
        _ = new OverlaySurfaceHandle(() => platformUnmounts++, "platform-modal");
        var legacyHandle = new OverlaySurfaceHandle(() => { }, "legacy");

        history.AddPopup(typeof(HostComponent), popup, DateTime.Now, legacyHandle);

        history.ClearPopups();

        Assert.Equal(0, platformUnmounts);
        Assert.Empty(history.Popups);
    }
}
