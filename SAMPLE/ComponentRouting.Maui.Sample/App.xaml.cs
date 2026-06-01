namespace ComponentRouting.Maui.Sample;

public partial class App : Application
{
	private readonly Router router;

	public App(Router router)
	{
		this.router = router;
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new ContentPage());
		window.Created += HandleWindowCreated;
		window.Stopped += async (_, _) => await router.DispatchSleep();
		window.Destroying += async (_, _) => await router.DispatchDestroy();
		return window;
	}

	private void HandleWindowCreated(object? sender, EventArgs e)
	{
		_ = router.PresentComponent<Components.Root.SampleTabbedRootComponent, bool, bool>(true);
	}
}
