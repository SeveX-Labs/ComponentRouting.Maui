using ComponentRouting.Maui.Abstraction;

namespace ComponentRouting.Maui.Sample.Presenters.Pushables;

public partial class WizardConfirmPage : PushablePresenter
{
    private Func<Task>? complete;

    public WizardConfirmPage()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message, Func<Task> complete)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.complete = complete;
    }

    private async void HandleCompleteClicked(object? sender, EventArgs e)
    {
        if (complete is null)
            return;

        var button = sender as Button;
        if (button is not null)
            button.IsEnabled = false;

        try
        {
            await complete();
        }
        finally
        {
            if (button is not null)
                button.IsEnabled = true;
        }
    }
}
