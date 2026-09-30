using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Enums;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.PublishRecipe;

public sealed record PublishRecipeCommand(Guid Id) : IRequest;

public sealed class PublishRecipeCommandHandler(
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<PublishRecipeCommand>
{
    public async Task Handle(PublishRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await unitOfWork.Recipes.GetByIdForPublishingAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công thức.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        if (recipe.Status != RecipeStatus.Draft)
        {
            throw new ConflictException("RECIPE_STATUS_TRANSITION_INVALID", "Chỉ công thức Draft mới có thể được xuất bản.");
        }

        recipe.Publish();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateListAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}
