using Microsoft.Maui.Controls;
using NGettext;
using ComponentRouting.Maui;
using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Model;
using ComponentRouting.Maui.Model.Core;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Routing;
using ComponentRouting.Maui.Service.Core;

namespace ComponentRouting.Maui.Tests;

public sealed class TestPageComponent : TestComponentBase
{
}

public sealed class UnsupportedRoutableComponent : TestComponentBase, RoutableComponent<object, object>
{
    public Task<Presenter> Prepare(object state) => Task.FromResult<Presenter>(new TestPresenter());
    public Task<object> Present() => Task.FromResult<object>(new object());
}

public class TestOverlayComponent : OverlayComponent<object, object>
{
    public override View? Backdrop => null;

    protected override Presenter CreatePresenter() => new TestPresenter();
    protected override Task Initialize(object state) => Task.CompletedTask;
    protected override Task Configure(object state) => Task.CompletedTask;
    protected override Task PresentInternal() => Task.CompletedTask;
}

public abstract class TestOverlayBase : TestOverlayComponent
{
}

public sealed class TestIndirectOverlayComponent : TestOverlayBase
{
}

public sealed class CountingOverlayComponent : TestOverlayComponent
{
    public int UnpresentCount { get; private set; }

    public override bool Unpresent()
    {
        UnpresentCount++;
        return base.Unpresent();
    }
}

public class TestSnackbarComponent : SnackbarComponent
{
    protected override Presenter CreatePresenter() => new TestSnackbarPresenter();
    protected override Task PresentInternal() => Task.CompletedTask;
}

public abstract class TestSnackbarBase : TestSnackbarComponent
{
}

public sealed class TestIndirectSnackbarComponent : TestSnackbarBase
{
}

public sealed class CountingSnackbarComponent : TestSnackbarComponent
{
    public int UnpresentCount { get; private set; }

    public override bool Unpresent()
    {
        UnpresentCount++;
        return base.Unpresent();
    }
}

public sealed class TestRootComponent : RootComponent
{
    protected override Presenter CreatePresenter() => new TestPresenter();
    protected override Task Initialize(bool state) => Task.CompletedTask;
    protected override Task Configure(bool state) => Task.CompletedTask;
    protected override Task PresentInternal() => Task.CompletedTask;
}

public sealed class HostComponent : TestComponentBase
{
    public HostPresenter Host { get; } = new();
    public override Presenter? Presenter => Host;
}

public abstract class TestComponentBase : Component
{
    public virtual Presenter? Presenter { get; }
    public void Dispose() { }
    public virtual void Resume() { }
    public virtual bool Unpresent() => true;
}

public sealed class TestPresenter : Presenter
{
}

public sealed class TestOverlayPresenter : AbsoluteLayout, Presenter
{
}

public sealed class HostPresenter : AbsoluteLayout, Presenter, OverlayHost
{
    public AbsoluteLayout? OverlayContainer => this;
}

public sealed class TestSnackbarPresenter : SnackbarPresenter
{
    public override Task Initialize(string text) => Task.CompletedTask;
}

public sealed class TestComponentFactory : ComponentFactory
{
    public C CreateComponent<C>() where C : Component => Activator.CreateInstance<C>();
    public Component CreateComponent(Type componentType) => (Component)Activator.CreateInstance(componentType)!;
    public Component CreateComponent(string componentTypeName) => throw new NotSupportedException();
    public void DisposeComponent(Component component) => component.Dispose();
}

public sealed class InstanceComponentFactory : ComponentFactory
{
    private readonly Dictionary<Type, Component> _components = new();

    public InstanceComponentFactory(params Component[] components)
    {
        foreach (var component in components)
            _components[component.GetType()] = component;
    }

    public C CreateComponent<C>() where C : Component => (C)_components[typeof(C)];
    public Component CreateComponent(Type componentType) => _components[componentType];
    public Component CreateComponent(string componentTypeName) => throw new NotSupportedException();
    public void DisposeComponent(Component component) => component.Dispose();
}

public sealed class ConfigurableTestRouter : AbstractRouter
{
    public ConfigurableTestRouter(ComponentFactory componentFactory)
        : base(componentFactory, new TestCatalogProvider(), new TestSafeAreaInsetsService())
    {
    }

    public override RootComponent RootComponent { get; } = new TestRootComponent();

    protected override bool CanNavigateBack(Component component) => true;
}

public sealed class ResumeCountingComponent : TestComponentBase, AppLifecycleAwareComponent
{
    public int ResumeCount { get; private set; }

    public override void Resume()
    {
        ResumeCount++;
    }

    public Task HandleAppSleepAsync() => Task.CompletedTask;
    public Task HandleAppDestroyAsync() => Task.CompletedTask;
}

public sealed class TestCatalogProvider : CatalogProvider
{
    public Task<ICatalog> GetLocalCatalog() => throw new NotSupportedException();
    public Task<string> GetCatalogTwoLetterIsoLanguageName(bool skipFallbackValidation = false) => throw new NotSupportedException();
    public void Dispose() { }
}

public sealed class TestSafeAreaInsetsService : SafeAreaInsetsService
{
    public SafeAreaInsets GetSafeAreaInsets(bool getCached = false) => new(0, 0, 0, 0);
}
