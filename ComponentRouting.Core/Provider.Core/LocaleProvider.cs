using System.Threading.Tasks;

namespace ComponentRouting.Maui.Provider.Core
{
    public interface LocaleProvider
    {
        Task<string> GetTwoLetterIsoLanguageName();
        Task<string> GetTwoLetterIsoFallbackLanguageName();
    }
}
