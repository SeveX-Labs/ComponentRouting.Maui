using NGettext;

namespace ComponentRouting.Maui.Model.Core
{
    public interface LocalizableEntity
    {
        void ApplyLocalization(ICatalog catalog);
    }
}

