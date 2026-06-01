using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Components.Base;
using ComponentRouting.Maui.Sample.Components.Flyout;
using ComponentRouting.Maui.Sample.Presenters.Root;

namespace ComponentRouting.Maui.Sample.Components.Root;

public sealed class SampleFlyoutRootComponent : RootComponent
{
    private readonly ComponentFactory componentFactory;
    private readonly Router router;

    public SampleFlyoutRootComponent(ComponentFactory componentFactory, Router router)
    {
        this.componentFactory = componentFactory;
        this.router = router;
    }

    protected override Presenter CreatePresenter()
    {
        return new SampleFlyoutRootPage(router);
    }

    protected override async Task Configure(bool state)
    {
        var rootPage = (SampleFlyoutRootPage)Presenter!;
        rootPage.HomeComponent = await BindFlyout<SampleFlyoutHomeComponent, bool>(rootPage.HomePage, true);
        rootPage.CustomersComponent = await BindFlyout<SampleFlyoutCustomersComponent, bool>(rootPage.CustomersPage, true);
        rootPage.SettingsComponent = await BindFlyout<SampleFlyoutSettingsComponent, bool>(rootPage.SettingsPage, true);
    }

    protected override Task Initialize(bool state)
    {
        ((SampleFlyoutRootPage)Presenter!).SelectInitialPage();
        return Task.CompletedTask;
    }

    private async Task<TComponent> BindFlyout<TComponent, TState>(Presenter presenter, TState state)
        where TComponent : SampleFlyoutComponent<TState>
    {
        var component = componentFactory.CreateComponent<TComponent>();
        component.InsertPresenter(presenter);
        await component.Prepare(state);
        return component;
    }
}
