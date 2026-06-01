using ComponentRouting.Maui.Provider.Core;
using NGettext;

namespace ComponentRouting.Maui.Sample.Services;

public sealed class SampleCatalogProvider : CatalogProvider
{
    public Task<ICatalog> GetLocalCatalog()
    {
        return Task.FromResult<ICatalog>(new Catalog());
    }

    public Task<string> GetCatalogTwoLetterIsoLanguageName(bool skipFallbackValidation = false)
    {
        return Task.FromResult("en");
    }

    public void Dispose()
    {
    }
}
