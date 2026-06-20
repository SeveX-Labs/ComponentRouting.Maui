using ComponentRouting.Maui.Chrome;
using Microsoft.Maui.Hosting;

namespace ComponentRouting.Maui.Ioc;

public static class ComponentRoutingMauiAppBuilderExtensions
{
    public static MauiAppBuilder UseComponentRoutingMauiPlatformChrome(this MauiAppBuilder builder)
    {
        builder.Services.AddComponentRoutingMauiPlatformChrome();

#if IOS
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(
                typeof(ComponentRoutingStatusBarNavigationPage),
                typeof(ComponentRoutingStatusBarNavigationRenderer));
        });
#endif
        return builder;
    }
}
