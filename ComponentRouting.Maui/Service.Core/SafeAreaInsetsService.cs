using ComponentRouting.Maui.Model;

namespace ComponentRouting.Maui.Service.Core
{
    public interface SafeAreaInsetsService
    {
        SafeAreaInsets GetSafeAreaInsets(bool getCached = false);
    }
}
