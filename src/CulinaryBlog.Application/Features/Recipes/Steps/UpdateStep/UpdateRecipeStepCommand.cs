using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Steps.UpdateStep;

public sealed record UpdateRecipeStepCommand(Guid RecipeId, Guid StepId, UpdateRecipeStepRequest Request) : IRequest<RecipeStepResponseDto>;

public sealed class UpdateRecipeStepCommandHandler(
    IRecipeRepository recipeRepository,
    IUnitOfWork unitOfWork,
    IRecipeAuthorizationService authorizationService,
    IRecipeCacheInvalidator cacheInvalidator)
    : IRequestHandler<UpdateRecipeStepCommand, RecipeStepResponseDto>
{
    public async Task<RecipeStepResponseDto> Handle(UpdateRecipeStepCommand command, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetByIdWithDetailsAsync(command.RecipeId, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy công thức với Id '{command.RecipeId}'.");

        await authorizationService.EnsureCanUpdateAsync(recipe, cancellationToken);

        var step = recipe.Steps.FirstOrDefault(s => s.Id == command.StepId && !s.IsDeleted)
            ?? throw new NotFoundException($"Không tìm thấy bước làm với Id '{command.StepId}'.");

        step.Update(
            command.Request.Title,
            command.Request.Description,
            command.Request.TimerMinutes,
            command.Request.ImageUrl);

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
