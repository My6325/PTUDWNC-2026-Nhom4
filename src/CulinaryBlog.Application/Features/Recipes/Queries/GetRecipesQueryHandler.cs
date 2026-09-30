using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Common;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

public sealed class GetRecipesQueryHandler(IRecipeRepository recipeRepository)
    : IRequestHandler<GetRecipesQuery, PaginatedResult<RecipeListDto>>
{
    public Task<PaginatedResult<RecipeListDto>> Handle(
        GetRecipesQuery request,
        CancellationToken cancellationToken)
    {
        return recipeRepository.SearchRecipesAsync(
            request.SearchTerm,
            request.CategoryId,
            request.Difficulty,
            string.IsNullOrWhiteSpace(request.SortBy) ? "newest" : request.SortBy,
            request.PageIndex,
            request.PageSize,
            cancellationToken);
    }
}
