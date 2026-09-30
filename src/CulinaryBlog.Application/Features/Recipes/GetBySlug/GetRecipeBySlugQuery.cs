using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Domain.Enums;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.GetBySlug;

public sealed record GetRecipeBySlugQuery(string Slug) : IRequest<RecipeDetailDto>;

public sealed class GetRecipeBySlugQueryHandler(
    IRecipeRepository recipeRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetRecipeBySlugQuery, RecipeDetailDto>
{
    public async Task<RecipeDetailDto> Handle(GetRecipeBySlugQuery request, CancellationToken cancellationToken)
    {
        var recipe = await recipeRepository.GetDetailBySlugAsync(request.Slug, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công thức.");

        if (recipe.Status != RecipeStatus.Published &&
            !(currentUser.IsAuthenticated && (currentUser.IsInRole("Admin") || currentUser.UserId == recipe.AuthorId)))
        {
            throw new ForbiddenException("Bạn không có quyền xem công thức này.");
        }

        return recipe;
    }
}
