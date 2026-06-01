using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Devices;

namespace ComponentRouting.Maui.Abstraction
{
    public abstract class RootPresenter : ContentPage, Presenter
    {
        #region access methods

        public bool UsingSafeArea()
        {
#pragma warning disable CS0618
            // Preserve legacy iOS platform-configuration behavior while hiding the framework warning locally.
            return (DeviceInfo.Platform == DevicePlatform.iOS) ? On<iOS>().UsingSafeArea() : false;
#pragma warning restore CS0618
        }

        #endregion
    }
}
