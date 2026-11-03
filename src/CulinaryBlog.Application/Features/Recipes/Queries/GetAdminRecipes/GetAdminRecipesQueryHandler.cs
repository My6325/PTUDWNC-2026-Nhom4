using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Common;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries.GetAdminRecipes;

public sealed class GetAdminRecipesQueryHandler(IRecipeRepository recipeRepository)
    : IRequestHandler<GetAdminRecipesQuery, PaginatedResult<AdminRecipeListDto>>
{
    public Task<PaginatedResult<AdminRecipeListDto>> Handle(
        GetAdminRecipesQuery request,
        CancellationToken cancellationToken)
    {
        return recipeRepository.GetAdminRecipesAsync(
            request.SearchTerm,
            request.Status,
            request.OnlyDeleted ?? false,
            request.PageIndex ?? 1,
            request.PageSize ?? 15,
            cancellationToken);
    }
}
