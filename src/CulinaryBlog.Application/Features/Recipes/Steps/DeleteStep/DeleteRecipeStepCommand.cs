using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Steps.DeleteStep;

public sealed record DeleteRecipeStepCommand(Guid RecipeId, Guid StepId) : IRequest;

public sealed class DeleteRecipeStepCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<DeleteRecipeStepCommand>
{
    public async Task Handle(DeleteRecipeStepCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.RecipeId}'.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        var stepExists = recipe.Steps.Any(s => s.Id == command.StepId && !s.IsDeleted);
        if (!stepExists)
        {
            throw new NotFoundException($"Không tìm thấy bước làm với Id '{command.StepId}'.");
        }

        recipe.RemoveStep(command.StepId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}
