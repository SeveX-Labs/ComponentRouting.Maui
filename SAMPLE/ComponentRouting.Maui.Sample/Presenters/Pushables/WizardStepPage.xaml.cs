using ComponentRouting.Maui.Abstraction;

namespace ComponentRouting.Maui.Sample.Presenters.Pushables;

public partial class WizardStepPage : PushablePresenter
{
    private Func<Task>? continueAction;

    public WizardStepPage()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message, Func<Task> continueAction)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.continueAction = continueAction;
    }

    private async void HandleContinueClicked(object? sender, EventArgs e)
    {
        if (continueAction is not null)
            await continueAction();
    }
}
