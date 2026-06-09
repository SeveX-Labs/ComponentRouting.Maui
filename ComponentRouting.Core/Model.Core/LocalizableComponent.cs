using NGettext;
using System.Threading.Tasks;

namespace ComponentRouting.Maui.Model.Core;

public interface LocalizableComponent
{
    Task ApplyLocalization(ICatalog catalog);
}