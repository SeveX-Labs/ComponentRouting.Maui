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
		var window = new Window(new ContentPage())
			.UseComponentRoutingMauiLifecycle(router);
		window.Created += HandleWindowCreated;
		window.Stopped += async (_, _) => await router.DispatchSleep();
		return window;
	}

	private void HandleWindowCreated(object? sender, EventArgs e)
	{
		_ = router.PresentComponent<Components.Root.SampleModeRootComponent, bool, bool>(true);
	}
}
