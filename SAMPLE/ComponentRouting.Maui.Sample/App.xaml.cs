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
		router.BeginNewRuntime();

		var window = new Window(new ContentPage());
		window.Created += HandleWindowCreated;
		window.Stopped += async (_, _) => await router.DispatchSleep();
		window.Destroying += (_, _) => _ = router.ShutdownAsync(new RouterShutdownOptions
		{
			Reason = RouterShutdownReason.WindowDestroying,
			DisconnectMauiPageTree = true
		});
		return window;
	}

	private void HandleWindowCreated(object? sender, EventArgs e)
	{
		router.BeginNewRuntime();
		_ = router.PresentComponent<Components.Root.SampleModeRootComponent, bool, bool>(true);
	}
}
