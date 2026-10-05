using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Images.DeleteImage;

public sealed record DeleteRecipeImageCommand(Guid RecipeId, Guid ImageId) : IRequest;

public sealed class DeleteRecipeImageCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    ISupabaseStorageService storageService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<DeleteRecipeImageCommand>
{
    public async Task Handle(DeleteRecipeImageCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.RecipeId}'.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        var image = recipe.Images.FirstOrDefault(img => img.Id == command.ImageId && !img.IsDeleted)
            ?? throw new NotFoundException($"Không tìm thấy hình ảnh với Id '{command.ImageId}'.");

        // 1. Xóa tệp vật lý trên Supabase Storage
        if (!string.IsNullOrWhiteSpace(image.OriginalUrl))
        {
            await storageService.DeleteFileAsync(image.OriginalUrl, cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(image.MediumUrl))
        {
            await storageService.DeleteFileAsync(image.MediumUrl, cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(image.ThumbnailUrl))
        {
            await storageService.DeleteFileAsync(image.ThumbnailUrl, cancellationToken);
        }

        // 2. Xóa bản ghi trong Domain (nếu là ảnh chính thì tự động gán ảnh tiếp theo làm ảnh chính)
        recipe.RemoveImage(command.ImageId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateListAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}
