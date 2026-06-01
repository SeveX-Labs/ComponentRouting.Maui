namespace ComponentRouting.Maui.Sample.Presenters.Modals;

public partial class DetailsPage : ContentPage, Presenter
{
    private Action? completeTrue;
    private Action? completeFalse;

    public DetailsPage()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message, Action completeTrue, Action completeFalse)
    {
        Title = title;
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.completeTrue = completeTrue;
        this.completeFalse = completeFalse;
    }

    private void HandleTrueClicked(object? sender, EventArgs e)
    {
        completeTrue?.Invoke();
    }

    private void HandleFalseClicked(object? sender, EventArgs e)
    {
        completeFalse?.Invoke();
    }
}
