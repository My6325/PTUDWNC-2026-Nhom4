using CulinaryBlog.Domain.Common;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

public sealed class GetRecipesQuery : IRequest<PaginatedResult<RecipeListDto>>
{
    public string? SearchTerm { get; init; }

    public Guid? CategoryId { get; init; }

    public string? Difficulty { get; init; }

    public int? MinCookTimeMinutes { get; init; }

    public int? MaxCookTimeMinutes { get; init; }

    public int? MinServings { get; init; }

    public int? MaxServings { get; init; }

    public string? SortBy { get; init; } = "newest";

    public int PageIndex { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}
