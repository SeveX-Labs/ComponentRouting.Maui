using NGettext;

namespace ComponentRouting.Maui.Model.Core
{
    public interface LocalizableElement
    {
        void ApplyLocalization(ICatalog catalog);
    }
}
