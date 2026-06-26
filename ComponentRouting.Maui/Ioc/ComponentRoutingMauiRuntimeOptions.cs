namespace ComponentRouting.Maui.Ioc;

public sealed class ComponentRoutingMauiRuntimeOptions
{
    public bool UseAutomaticPlatformLifecycle { get; set; }

    public RouterShutdownOptions AndroidOnDestroyShutdownOptions { get; set; } =
        CreateDefaultWindowDestroyingShutdownOptions();

    public ComponentRoutingMauiRuntimeOptions EnableAutomaticPlatformLifecycle()
    {
        UseAutomaticPlatformLifecycle = true;
        return this;
    }

    internal RouterShutdownOptions GetAndroidOnDestroyShutdownOptions()
    {
        return AndroidOnDestroyShutdownOptions ?? CreateDefaultWindowDestroyingShutdownOptions();
    }

    private static RouterShutdownOptions CreateDefaultWindowDestroyingShutdownOptions()
    {
        return new RouterShutdownOptions
        {
            Reason = RouterShutdownReason.WindowDestroying,
            DisconnectMauiPageTree = true
        };
    }
}
