using CulinaryBlog.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Categories.Queries;

public sealed record GetCategoryDeleteBlockers : IRequest<List<Guid>>;

public sealed class GetCategoryDeleteBlockersHandler(IApplicationDbContext db)
    : IRequestHandler<GetCategoryDeleteBlockers, List<Guid>>
{
    public Task<List<Guid>> Handle(GetCategoryDeleteBlockers request, CancellationToken cancellationToken) =>
        db.Recipes.Where(recipe => recipe.CategoryId.HasValue)
            .Select(recipe => recipe.CategoryId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
}
