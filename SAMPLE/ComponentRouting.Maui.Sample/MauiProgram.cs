using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ComponentRouting.Maui;
using ComponentRouting.Maui.Ioc;
using ComponentRouting.Maui.Provider.Core;
using ComponentRouting.Maui.Sample.Routing;
using ComponentRouting.Maui.Sample.Services;
using ComponentRouting.Maui.Service.Core;

namespace ComponentRouting.Maui.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddComponentRoutingMaui(typeof(SampleRouter).Assembly);
		builder.Services.AddSingleton<SampleRouter>();
		builder.Services.AddSingleton<Router>(sp => sp.GetRequiredService<SampleRouter>());
		builder.Services.AddSingleton<CatalogProvider, SampleCatalogProvider>();
		builder.Services.AddSingleton<SafeAreaInsetsService, SampleSafeAreaInsetsService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
