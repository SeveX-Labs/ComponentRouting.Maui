namespace ComponentRouting.Maui.Sample.Presenters.Pages;

public partial class LoginPage : ContentPage, Presenter
{
    private Action? complete;
    private Action? cancel;

    public LoginPage()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message, Action complete, Action cancel)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.complete = complete;
        this.cancel = cancel;
    }

    private void HandleCompleteClicked(object? sender, EventArgs e)
    {
        complete?.Invoke();
    }

    private void HandleCancelClicked(object? sender, EventArgs e)
    {
        cancel?.Invoke();
    }
}
