using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Components.Base;
using ComponentRouting.Maui.Sample.Components.Tabs;
using ComponentRouting.Maui.Sample.Presenters.Root;

namespace ComponentRouting.Maui.Sample.Components.Root;

public sealed class SampleTabbedRootComponent : RootComponent
{
    private readonly ComponentFactory componentFactory;
    private readonly Router router;

    public SampleTabbedRootComponent(ComponentFactory componentFactory, Router router)
    {
        this.componentFactory = componentFactory;
        this.router = router;
    }

    protected override Presenter CreatePresenter()
    {
        return new SampleTabbedRootPage(router);
    }

    protected override async Task Configure(bool state)
    {
        var rootPage = (SampleTabbedRootPage)Presenter!;
        rootPage.HomeComponent = await BindTab<HomeComponent, bool>(rootPage.HomePage, true);
        rootPage.AboutComponent = await BindTab<AboutComponent, bool>(rootPage.AboutPage, true);
    }

    protected override Task Initialize(bool state)
    {
        ((SampleTabbedRootPage)Presenter!).SelectInitialTab();
        return Task.CompletedTask;
    }

    private async Task<TComponent> BindTab<TComponent, TState>(Presenter presenter, TState state)
        where TComponent : SampleTabComponent<TState>
    {
        var component = componentFactory.CreateComponent<TComponent>();
        component.InsertPresenter(presenter);
        await component.Prepare(state);
        return component;
    }
}
