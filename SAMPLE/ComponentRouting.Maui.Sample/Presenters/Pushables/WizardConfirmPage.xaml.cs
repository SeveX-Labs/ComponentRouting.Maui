using ComponentRouting.Maui.Abstraction;

namespace ComponentRouting.Maui.Sample.Presenters.Pushables;

public partial class WizardConfirmPage : PushablePresenter
{
    private Action? complete;

    public WizardConfirmPage()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message, Action complete)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.complete = complete;
    }

    private void HandleCompleteClicked(object? sender, EventArgs e)
    {
        complete?.Invoke();
    }
}
