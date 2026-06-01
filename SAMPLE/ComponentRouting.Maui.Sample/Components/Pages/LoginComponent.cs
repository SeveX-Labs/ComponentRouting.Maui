using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Models;
using ComponentRouting.Maui.Sample.Presenters.Pages;

namespace ComponentRouting.Maui.Sample.Components.Pages;

public sealed class LoginComponent : PageComponent<LoginComponent.ComponentState, LoginResult>
{
    public readonly record struct ComponentState(string Title, string Message);

    protected override Presenter CreatePresenter()
    {
        return new LoginPage();
    }

    protected override Task Configure(ComponentState state)
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize(ComponentState state)
    {
        ((LoginPage)Presenter!).Initialize(
            state.Title,
            state.Message,
            () => CompletionSource?.TrySetResult(LoginResult.SignedIn),
            () => CompletionSource?.TrySetResult(LoginResult.Cancelled));
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }
}
