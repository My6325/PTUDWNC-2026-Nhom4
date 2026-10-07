using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Delete;

public sealed record DeleteRecipeCommand(Guid Id) : IRequest;

public sealed class DeleteRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<DeleteRecipeCommand>
{
    public async Task Handle(DeleteRecipeCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.Id}'.");

        await authorizationService.EnsureCanDeleteAsync(recipe, cancellationToken);

        // Thực hiện xóa mềm (Soft Delete) Recipe và toàn bộ quan hệ phụ thuộc (Steps, Ingredients, Images)
        recipe.SoftDelete();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Vô hiệu hóa cache danh sách và cache chi tiết
        await cacheInvalidator.InvalidateListAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}
