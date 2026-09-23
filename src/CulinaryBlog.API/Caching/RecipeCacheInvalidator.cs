using CulinaryBlog.Application.Contracts;
using Microsoft.AspNetCore.OutputCaching;

namespace CulinaryBlog.API.Caching;

public sealed class RecipeCacheInvalidator(IOutputCacheStore cacheStore) : IRecipeCacheInvalidator
{
    public async Task InvalidateListAsync(CancellationToken cancellationToken)
    {
        await cacheStore.EvictByTagAsync("recipes", cancellationToken);
    }

    public async Task InvalidateDetailAsync(string slug, CancellationToken cancellationToken)
    {
        await cacheStore.EvictByTagAsync($"recipe:{slug}", cancellationToken);
    }
}
