using ComponentRouting.Maui.Routing;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class OverlaySurfaceOwnershipTests
{
    [Fact]
    public void GetInheritedSurface_returns_root_when_owner_is_missing()
    {
        var registry = new OverlaySurfaceOwnershipRegistry();

        var surface = registry.GetInheritedSurface(null);

        Assert.Equal(OverlaySurfaceKind.Root, surface);
    }

    [Fact]
    public void GetInheritedSurface_returns_root_when_owner_is_unknown()
    {
        var registry = new OverlaySurfaceOwnershipRegistry();

        var surface = registry.GetInheritedSurface(new TestComponent());

        Assert.Equal(OverlaySurfaceKind.Root, surface);
    }

    [Fact]
    public void GetInheritedSurface_returns_registered_owner_surface()
    {
        var registry = new OverlaySurfaceOwnershipRegistry();
        var modal = new TestComponent();

        registry.Set(modal, OverlaySurfaceKind.Modal);

        Assert.Equal(OverlaySurfaceKind.Modal, registry.GetInheritedSurface(modal));
    }

    [Fact]
    public void GetInheritedSurface_preserves_modal_ownership_for_push_inside_modal()
    {
        var registry = new OverlaySurfaceOwnershipRegistry();
        var modal = new TestComponent();

        registry.Set(modal, OverlaySurfaceKind.Modal);
        var inheritedSurface = registry.GetInheritedSurface(modal);

        Assert.Equal(OverlaySurfaceKind.Modal, inheritedSurface);
    }

    [Fact]
    public void Remove_deletes_owner_surface_by_component_reference()
    {
        var registry = new OverlaySurfaceOwnershipRegistry();
        var component = new TestComponent();

        registry.Set(component, OverlaySurfaceKind.FullscreenModal);
        registry.Remove(component);

        Assert.False(registry.TryGet(component, out _));
    }

    [Fact]
    public void Clear_removes_all_owner_surfaces()
    {
        var registry = new OverlaySurfaceOwnershipRegistry();
        var first = new TestComponent();
        var second = new TestComponent();

        registry.Set(first, OverlaySurfaceKind.Root);
        registry.Set(second, OverlaySurfaceKind.Modal);
        registry.Clear();

        Assert.False(registry.TryGet(first, out _));
        Assert.False(registry.TryGet(second, out _));
    }

    [Fact]
    public void OverlaySurfaceDecisionPolicy_allows_root_platform_surface_for_root()
    {
        Assert.True(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.Root,
            hasPopupOwner: false,
            hasActiveNativeModal: false));
    }

    [Fact]
    public void OverlaySurfaceDecisionPolicy_rejects_root_platform_surface_for_non_root_surfaces()
    {
        Assert.False(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.Unknown,
            hasPopupOwner: false,
            hasActiveNativeModal: false));
    }

    [Fact]
    public void OverlaySurfaceDecisionPolicy_rejects_root_platform_surface_when_popup_owner_exists()
    {
        Assert.False(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.Root,
            hasPopupOwner: true,
            hasActiveNativeModal: false));
        Assert.False(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.Modal,
            hasPopupOwner: true,
            hasActiveNativeModal: true));
        Assert.False(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.FullscreenModal,
            hasPopupOwner: true,
            hasActiveNativeModal: true));
    }

    [Fact]
    public void OverlaySurfaceDecisionPolicy_rejects_root_platform_surface_when_native_modal_is_active()
    {
        Assert.False(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.Root,
            hasPopupOwner: false,
            hasActiveNativeModal: true));
    }

    [Fact]
    public void OverlaySurfaceDecisionPolicy_allows_modal_surfaces_when_native_modal_is_active()
    {
        Assert.True(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.Modal,
            hasPopupOwner: false,
            hasActiveNativeModal: true));
        Assert.True(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.FullscreenModal,
            hasPopupOwner: false,
            hasActiveNativeModal: true));
    }

    [Fact]
    public void OverlaySurfaceDecisionPolicy_rejects_modal_surfaces_when_native_modal_is_not_active()
    {
        Assert.False(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.Modal,
            hasPopupOwner: false,
            hasActiveNativeModal: false));
        Assert.False(OverlaySurfaceDecisionPolicy.CanUsePlatformSurface(
            OverlaySurfaceKind.FullscreenModal,
            hasPopupOwner: false,
            hasActiveNativeModal: false));
    }
}
