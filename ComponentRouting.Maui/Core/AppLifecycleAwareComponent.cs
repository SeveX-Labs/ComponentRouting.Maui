using System.Threading.Tasks;

namespace ComponentRouting.Maui;

public interface AppLifecycleAwareComponent
{
    Task HandleAppSleepAsync();

    Task HandleAppDestroyAsync();
}