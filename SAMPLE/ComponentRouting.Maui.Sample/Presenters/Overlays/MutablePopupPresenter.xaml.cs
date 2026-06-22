namespace ComponentRouting.Maui.Sample.Presenters.Overlays;

public partial class MutablePopupPresenter : Grid, Presenter
{
    private Action? update;
    private Action? unpresent;

    public MutablePopupPresenter()
    {
        InitializeComponent();
    }

    public void Initialize(string title, string message, Action update, Action unpresent)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        this.update = update;
        this.unpresent = unpresent;
    }

    public void UpdateMessage(string message)
    {
        MessageLabel.Text = message;
    }

    private void HandleUpdateClicked(object? sender, EventArgs e)
    {
        update?.Invoke();
    }

    private void HandleUnpresentClicked(object? sender, EventArgs e)
    {
        unpresent?.Invoke();
    }
}
