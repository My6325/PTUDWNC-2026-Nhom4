using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Enums;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.PublishRecipe;

public sealed record PublishRecipeCommand(Guid Id) : IRequest;

public sealed class PublishRecipeCommandHandler(
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator,
    ICurrentUserService currentUser)
    : IRequestHandler<PublishRecipeCommand>
{
    public async Task Handle(PublishRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await unitOfWork.Recipes.GetByIdForPublishingAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công thức.");

        if (!currentUser.IsAuthenticated || !currentUser.IsInRole("Admin"))
            throw new ForbiddenException("Chỉ Admin được duyệt xuất bản công thức.");
        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        if (recipe.Status != RecipeStatus.PendingApproval)
        {
            throw new ConflictException("RECIPE_STATUS_TRANSITION_INVALID", "Chỉ công thức đang chờ duyệt mới có thể được xuất bản.");
        }

        recipe.Publish();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateListAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}
