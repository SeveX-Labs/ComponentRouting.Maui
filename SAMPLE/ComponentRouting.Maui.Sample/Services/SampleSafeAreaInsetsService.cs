using ComponentRouting.Maui.Model;
using ComponentRouting.Maui.Service.Core;

namespace ComponentRouting.Maui.Sample.Services;

public sealed class SampleSafeAreaInsetsService : SafeAreaInsetsService
{
    public SafeAreaInsets GetSafeAreaInsets(bool getCached = false)
    {
        return new SafeAreaInsets(0, 0, 0, 0);
    }
}
