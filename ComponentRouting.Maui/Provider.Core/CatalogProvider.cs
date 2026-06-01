using NGettext;
using System;
using System.Threading.Tasks;

namespace ComponentRouting.Maui.Provider.Core
{
    public interface CatalogProvider : IDisposable
    {
        Task<ICatalog> GetLocalCatalog();
        Task<string> GetCatalogTwoLetterIsoLanguageName(bool skipFallbackValidation = false);
    }
}
