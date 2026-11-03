using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Delete;

public sealed record HardDeleteRecipeCommand(Guid Id) : IRequest;

public sealed class HardDeleteRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    ISupabaseStorageService storageService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<HardDeleteRecipeCommand>
{
    public async Task Handle(HardDeleteRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetDeletedByIdAsync(command.Id, cancellationToken)
            ?? await recipeRepository.GetByIdWithDetailsAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.Id}'.");

        // 1. Dọn dẹp ảnh trên Supabase Storage
        foreach (var image in recipe.Images)
        {
            if (!string.IsNullOrWhiteSpace(image.OriginalUrl))
            {
                try { await storageService.DeleteFileAsync(image.OriginalUrl, cancellationToken); } catch { }
            }
            if (!string.IsNullOrWhiteSpace(image.MediumUrl))
            {
                try { await storageService.DeleteFileAsync(image.MediumUrl, cancellationToken); } catch { }
            }
            if (!string.IsNullOrWhiteSpace(image.ThumbnailUrl))
            {
                try { await storageService.DeleteFileAsync(image.ThumbnailUrl, cancellationToken); } catch { }
            }
        }

        // 2. Xóa cứng khỏi CSDL (Cascade delete Steps, Ingredients, Images)
        await recipeRepository.DeletePermanentAsync(recipe, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Vô hiệu hóa cache
        await cacheInvalidator.InvalidateListAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}
