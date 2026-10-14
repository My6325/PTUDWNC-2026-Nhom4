using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Domain.Entities;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Steps.AddStep;

public sealed record AddRecipeStepCommand(Guid RecipeId, AddRecipeStepRequest Request) : IRequest<RecipeStepResponseDto>;

public sealed class AddRecipeStepCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<AddRecipeStepCommand, RecipeStepResponseDto>
{
    public async Task<RecipeStepResponseDto> Handle(AddRecipeStepCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.RecipeId}'.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        var request = command.Request;
        var nextStepNumber = recipe.Steps.Count(s => !s.IsDeleted) + 1;

        var step = RecipeStep.Create(
            recipe.Id,
            nextStepNumber,
            request.Title,
            request.Description,
            request.TimerMinutes,
            request.ImageUrl);

        recipe.AddStep(step);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateDetailAsync(recipe.Slug, cancellationToken);

        return new RecipeStepResponseDto(
            step.Id,
            step.RecipeId,
            step.StepNumber,
            step.Title,
            step.Description,
            step.TimerMinutes,
            step.ImageUrl);
    }
}
