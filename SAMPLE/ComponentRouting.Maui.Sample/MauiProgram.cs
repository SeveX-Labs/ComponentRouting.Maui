using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ComponentRouting.Maui;
using ComponentRouting.Maui.Chrome;
using ComponentRouting.Maui.Ioc;
using ComponentRouting.Maui.Sample.Components.Pages;
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
			.UseComponentRoutingMauiPlatformChrome()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services
			.AddComponentRoutingMaui(
				new[] { typeof(SampleRouter).Assembly },
				configureChrome: chrome =>
				{
					var normalChromeColor = Color.FromArgb("#334155");
					var normalChrome = new ComponentChromeOptions
					{
						StatusBarBackgroundColor = normalChromeColor,
						NavigationBarBackgroundColor = normalChromeColor,
						ActionBarBackgroundColor = normalChromeColor,
						WindowBackgroundColor = normalChromeColor,
						StatusBarForeground = ChromeForeground.LightContent,
						NavigationBarForeground = ChromeForeground.LightContent,
						ActionBarTextColor = Colors.White,
						EdgeToEdge = false,
						DecorFitsSystemWindows = true,
						DisplayCutoutMode = ComponentDisplayCutoutMode.Default
					};

					chrome.GlobalDefaults = normalChrome;
					chrome.PageDefaults = normalChrome;
					chrome.PushableDefaults = normalChrome;
					chrome.ModalDefaults = normalChrome;
					chrome.FullscreenModalDefaults = new ComponentChromeOptions
					{
						StatusBarBackgroundColor = Colors.Transparent,
						NavigationBarBackgroundColor = Colors.Transparent,
						WindowBackgroundColor = Colors.Transparent,
						StatusBarForeground = ChromeForeground.LightContent,
						NavigationBarForeground = ChromeForeground.LightContent,
						ActionBarTextColor = Colors.White,
						EdgeToEdge = true,
						DecorFitsSystemWindows = false,
						DisplayCutoutMode = ComponentDisplayCutoutMode.Always,
						StatusBarContrastEnforced = false,
						NavigationBarContrastEnforced = false
					};
					chrome.ComponentOverrides[typeof(LoginComponent)] = new ComponentChromeOptions
					{
						StatusBarForeground = ChromeForeground.DarkContent,
						NavigationBarForeground = ChromeForeground.DarkContent,
						ActionBarTextColor = Colors.Black
					};
				});
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
