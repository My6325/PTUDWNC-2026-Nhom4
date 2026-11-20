using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Features.Categories.Common;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.API.Caching;

public sealed class RecipeCacheInvalidator(IOutputCacheStore cacheStore, IMemoryCache memoryCache) : IRecipeCacheInvalidator
{
    public async Task InvalidateListAsync(CancellationToken cancellationToken)
    {
        memoryCache.Remove(CategoryCacheKeys.All);
        await cacheStore.EvictByTagAsync("recipes", cancellationToken);
    }

    public async Task InvalidateDetailAsync(string slug, CancellationToken cancellationToken)
    {
        await cacheStore.EvictByTagAsync($"recipe:{slug}", cancellationToken);
    }
}
