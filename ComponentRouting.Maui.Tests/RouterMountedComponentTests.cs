using System.Collections;
using System.Reflection;
using Microsoft.Maui.Controls;
using ComponentRouting.Maui;
using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Model.Core;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Routing;
using ComponentRouting.Maui.Service.Core;
using Xunit;

namespace ComponentRouting.Maui.Tests;

public class RouterMountedComponentTests
{
    [Fact]
    public void GetMountedComponents_searches_only_popups_and_snackbars()
    {
        var router = new TestRouter();
        var mounted = new TestPageComponent();
        var stacked = new TestPageComponent();
        var tab = new TestPageComponent();
        var flyout = new TestPageComponent();

        SetProperty(router, "MountedComponent", mounted);
        router.ComponentsStack.Add(stacked);
        SetProperty(router, "CurrentTabComponent", tab);
        SetProperty(router, "CurrentFlyoutComponent", flyout);

        Assert.Empty(router.GetMountedComponents<TestPageComponent>());
    }

    [Fact]
    public void GetMountedComponents_returns_compatible_popup_and_snackbar_instances_deduplicated_by_reference()
    {
        var router = new TestRouter();
        var popup = new TestOverlayComponent();
        var snackbar = new TestSnackbarComponent();

        AddHistoryItem(router, "Popups", popup);
        AddHistoryItem(router, "Snackbars", snackbar);
        AddHistoryItem(router, "Popups", snackbar);

        var mounted = router.GetMountedComponents<Component>();

        Assert.Equal(2, mounted.Count);
        Assert.Contains(popup, mounted);
        Assert.Contains(snackbar, mounted);
    }

    [Fact]
    public void GetMountedComponent_returns_null_when_no_match_exists()
    {
        var router = new TestRouter();

        Assert.Null(router.GetMountedComponent<TestOverlayComponent>());
    }

    [Fact]
    public void GetMountedComponent_returns_single_match()
    {
        var router = new TestRouter();
        var popup = new TestOverlayComponent();
        AddHistoryItem(router, "Popups", popup);

        Assert.Same(popup, router.GetMountedComponent<TestOverlayComponent>());
    }

    [Fact]
    public void GetMountedComponent_throws_when_multiple_matches_exist()
    {
        var router = new TestRouter();
        AddHistoryItem(router, "Popups", new TestOverlayComponent());
        AddHistoryItem(router, "Popups", new TestOverlayComponent());

        var ex = Assert.Throws<InvalidOperationException>(() => router.GetMountedComponent<TestOverlayComponent>());

        Assert.Contains(nameof(Router.GetMountedComponents), ex.Message);
    }

    [Fact]
    public async Task HandleComponentResult_removes_presented_overlay_history_by_reference()
    {
        var router = new TestRouter();
        var parent = new HostComponent();
        var first = new TestOverlayComponent();
        var second = new TestOverlayComponent();
        var firstPresenter = new TestOverlayPresenter();
        var secondPresenter = new TestOverlayPresenter();

        SetPresenter(first, firstPresenter);
        SetPresenter(second, secondPresenter);
        parent.Host.OverlayContainer!.Children.Add(firstPresenter);
        parent.Host.OverlayContainer!.Children.Add(secondPresenter);

        SetProperty(router, "MountedComponent", parent);
        AddHistoryItem(router, "Popups", first, parent.GetType());
        AddHistoryItem(router, "Popups", second, parent.GetType());

        await InvokeHandleComponentResult(router, second);

        var remaining = GetHistoryComponents(router, "Popups");
        Assert.Single(remaining);
        Assert.Same(first, remaining[0]);
    }

    private static void AddHistoryItem(TestRouter router, string fieldName, Component component, Type? parentType = null)
    {
        var historyType = typeof(AbstractRouter).GetNestedType("ComponentHistoryItem", BindingFlags.NonPublic)!;
        var item = Activator.CreateInstance(historyType, parentType ?? typeof(HostComponent), component)!;
        var list = (IList)typeof(AbstractRouter)
            .GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(router)!;

        list.Add(item);
    }

    private static List<Component> GetHistoryComponents(TestRouter router, string fieldName)
    {
        var list = (IEnumerable)typeof(AbstractRouter)
            .GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(router)!;

        return list
            .Cast<object>()
            .Select(item => (Component)item.GetType().GetProperty("Component")!.GetValue(item)!)
            .ToList();
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var backingField = typeof(AbstractRouter).GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (backingField is not null)
        {
            backingField.SetValue(instance, value);
            return;
        }

        typeof(AbstractRouter)
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(instance, value);
    }

    private static void SetPresenter(Component component, Presenter presenter)
    {
        typeof(AbstractComponent<object, object>)
            .GetProperty(nameof(Component.Presenter), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(component, presenter);
    }

    private static async Task InvokeHandleComponentResult(TestRouter router, Component component)
    {
        var task = (Task)typeof(AbstractRouter)
            .GetMethod("HandleComponentResult", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(router, new object[] { component })!;

        await task;
    }
}

public sealed class TestRouter : AbstractRouter
{
    public TestRouter()
        : base(new TestComponentFactory(), new TestCatalogProvider(), new TestSafeAreaInsetsService())
    {
    }

    public override bool IsSafeAreaInsetsApplyiable => false;
    public override RootComponent RootComponent { get; } = new TestRootComponent();

    protected override bool CanNavigateBack(Component component) => true;
}
