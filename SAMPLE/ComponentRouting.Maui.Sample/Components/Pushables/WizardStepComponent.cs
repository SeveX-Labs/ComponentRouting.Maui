using ComponentRouting.Maui.Abstraction;
using ComponentRouting.Maui.Sample.Models;
using ComponentRouting.Maui.Sample.Presenters.Pushables;

namespace ComponentRouting.Maui.Sample.Components.Pushables;

public sealed class WizardStepComponent : PushableComponent<WizardStepComponent.ComponentState, WizardResult>
{
    private readonly Router router;

    public WizardStepComponent(Router router)
    {
        this.router = router;
    }

    public readonly record struct ComponentState(string Title, string Message);

    protected override Presenter CreatePresenter()
    {
        return new WizardStepPage();
    }

    protected override Task Initialize(ComponentState state)
    {
        ((WizardStepPage)Presenter!).Initialize(state.Title, state.Message, Continue);
        return Task.CompletedTask;
    }

    protected override async Task PresentInternal()
    {
        await Task.CompletedTask;
    }

    public override void HandleBackTapped()
    {
        _ = router.DismissComponent<WizardStepComponent, ComponentState, WizardResult>(true);
        CompletionSource?.TrySetResult(WizardResult.Cancelled);
    }

    private async Task Continue()
    {
        var state = new WizardConfirmComponent.ComponentState(
            "Wizard confirmation",
            "This component was preloaded before presentation.");
        await router.PreloadComponent<WizardConfirmComponent, WizardConfirmComponent.ComponentState, WizardResult>(state);
        var result = await router.PresentComponent<WizardConfirmComponent, WizardConfirmComponent.ComponentState, WizardResult>(state);
        CompletionSource?.TrySetResult(result);
    }
}
