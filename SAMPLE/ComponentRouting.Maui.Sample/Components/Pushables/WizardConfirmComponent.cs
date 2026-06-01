using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Models;
using ComponentRouting.Maui.Sample.Presenters.Pushables;

namespace ComponentRouting.Maui.Sample.Components.Pushables;

public sealed class WizardConfirmComponent : PushableComponent<WizardConfirmComponent.ComponentState, WizardResult>
{
    private readonly Router router;

    public WizardConfirmComponent(Router router)
    {
        this.router = router;
    }

    public readonly record struct ComponentState(string Title, string Message);

    protected override Presenter CreatePresenter()
    {
        return new WizardConfirmPage();
    }

    protected override Task Initialize(ComponentState state)
    {
        ((WizardConfirmPage)Presenter!).Initialize(
            state.Title,
            state.Message,
            () => CompletionSource?.TrySetResult(WizardResult.Completed));
        return Task.CompletedTask;
    }

    protected override Task PresentInternal()
    {
        return Task.CompletedTask;
    }

    public override void HandleBackTapped()
    {
        _ = router.DismissComponent<WizardConfirmComponent, ComponentState, WizardResult>(true);
        CompletionSource?.TrySetResult(WizardResult.Cancelled);
    }
}
