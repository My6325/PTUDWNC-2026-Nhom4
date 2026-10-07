using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using MediatR;

namespace CulinaryBlog.Application.Features.Recipes.ChangeRecipeStatus;

public sealed record ArchiveRecipeCommand(Guid Id) : IRequest;
public sealed record UnarchiveRecipeCommand(Guid Id) : IRequest;
public sealed record UnarchiveRecipeToDraftCommand(Guid Id) : IRequest;
public sealed record UnpublishRecipeCommand(Guid Id) : IRequest;

public sealed class ArchiveRecipeCommandHandler(IUnitOfWork unitOfWork, IRecipeAuthorizationService authorization, IRecipeCacheInvalidator cache)
    : IRequestHandler<ArchiveRecipeCommand>
{
    public Task Handle(ArchiveRecipeCommand request, CancellationToken cancellationToken) =>
        ChangeAsync(request.Id, RecipeStatus.Published, RecipeStatus.Archived, "Chỉ công thức đã xuất bản mới có thể lưu trữ.", cancellationToken);

    private async Task ChangeAsync(Guid id, RecipeStatus from, RecipeStatus to, string error, CancellationToken ct)
    {
        var recipe = await unitOfWork.Recipes.GetByIdAsync(id, ct) ?? throw new NotFoundException("Không tìm thấy công thức.");
        await authorization.EnsureCanUpdateAsync(recipe, ct);
        if (recipe.Status != from) throw new ConflictException("RECIPE_STATUS_TRANSITION_INVALID", error);
        recipe.ChangeStatus(to);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.InvalidateListAsync(ct);
        await cache.InvalidateDetailAsync(recipe.Slug, ct);
    }
}

public sealed class UnarchiveRecipeCommandHandler(IUnitOfWork unitOfWork, IRecipeAuthorizationService authorization, IRecipeCacheInvalidator cache)
    : IRequestHandler<UnarchiveRecipeCommand>
{
    public async Task Handle(UnarchiveRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await unitOfWork.Recipes.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Không tìm thấy công thức.");
        await authorization.EnsureCanUpdateAsync(recipe, cancellationToken);
        if (recipe.Status != RecipeStatus.Archived) throw new ConflictException("RECIPE_STATUS_TRANSITION_INVALID", "Chỉ công thức đã lưu trữ mới có thể khôi phục.");
        recipe.ChangeStatus(RecipeStatus.Published);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateListAsync(cancellationToken);
        await cache.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}

public sealed class UnarchiveRecipeToDraftCommandHandler(IUnitOfWork unitOfWork, IRecipeAuthorizationService authorization, IRecipeCacheInvalidator cache)
    : IRequestHandler<UnarchiveRecipeToDraftCommand>
{
    public async Task Handle(UnarchiveRecipeToDraftCommand request, CancellationToken cancellationToken)
    {
        var recipe = await unitOfWork.Recipes.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy công thức.");
        await authorization.EnsureCanUpdateAsync(recipe, cancellationToken);
        if (recipe.Status != RecipeStatus.Archived)
        {
            throw new ConflictException("RECIPE_STATUS_TRANSITION_INVALID", "Chỉ công thức đã lưu trữ mới có thể khôi phục về bản nháp.");
        }

        recipe.ChangeStatus(RecipeStatus.Draft);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateListAsync(cancellationToken);
        await cache.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}

public sealed class UnpublishRecipeCommandHandler(IUnitOfWork unitOfWork, IRecipeAuthorizationService authorization, IRecipeCacheInvalidator cache)
    : IRequestHandler<UnpublishRecipeCommand>
{
    public async Task Handle(UnpublishRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await unitOfWork.Recipes.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Không tìm thấy công thức.");
        await authorization.EnsureCanUpdateAsync(recipe, cancellationToken);
        if (recipe.Status != RecipeStatus.Published) throw new ConflictException("RECIPE_STATUS_TRANSITION_INVALID", "Chỉ công thức đã xuất bản mới có thể gỡ xuất bản.");
        recipe.ChangeStatus(RecipeStatus.Draft);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await cache.InvalidateListAsync(cancellationToken);
        await cache.InvalidateDetailAsync(recipe.Slug, cancellationToken);
    }
}
