using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Recipes.Management;

public static class RecipeEditing
{
    public static void EnsureDraft(Recipe recipe)
    {
        if (recipe.IsDeleted || recipe.Status != RecipeStatus.Draft)
            throw new ConflictException("RECIPE_NOT_DRAFT", "Hãy thu hồi hoặc chuyển công thức về bản nháp trước khi chỉnh sửa.");
        // Child edits must also advance the aggregate's concurrency token.
        recipe.UpdatedAt = DateTime.UtcNow;
    }

    public static void CheckVersion(Recipe recipe, byte[]? version)
    {
        if (version is not { Length: 4 })
            throw new UnprocessableEntityException("ROW_VERSION_REQUIRED", "Cần phiên bản công thức hợp lệ. Vui lòng tải lại trang.");
        if (RecipeMappings.ToRowVersion(version) != recipe.RowVersion)
            throw new RecipeConcurrencyConflictException();
    }
}

public sealed record ManagedRecipeDto(RecipeDto Recipe, string? ReviewNote,
    RecipeNutrition Nutrition, IReadOnlyList<RecipeStepResponseDto> Steps,
    IReadOnlyList<RecipeIngredientResponseDto> Ingredients, IReadOnlyList<RecipeImageResponseDto> Images);
public sealed record ManagedRecipeListItem(Guid Id, string Title, string Slug, RecipeStatus Status,
    bool IsDeleted, string AuthorId, DateTime CreatedAt, string? ReviewNote, byte[] RowVersion);
public sealed record MyRecipesQuery(string? Status = null, bool Trash = false, bool ReviewQueue = false,
    int PageIndex = 1, int PageSize = 12) : IRequest<PaginatedResult<ManagedRecipeListItem>>;
public sealed record ManagedRecipeQuery(Guid Id) : IRequest<ManagedRecipeDto>;
public sealed record RecipeWorkflowCommand(Guid Id, string Action, byte[]? RowVersion, string? Reason = null) : IRequest;
public sealed record ReviewDecisionRequest(string Reason);

public sealed class MyRecipesValidator : AbstractValidator<MyRecipesQuery>
{
    public MyRecipesValidator()
    {
        RuleFor(x => x.PageIndex).InclusiveBetween(1, int.MaxValue / 50);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Status).Must(value => value is null ||
            Enum.TryParse<RecipeStatus>(value, true, out var status) && Enum.IsDefined(status));
    }
}

public sealed class MyRecipesHandler(IApplicationDbContext db, ICurrentUserService user)
    : IRequestHandler<MyRecipesQuery, PaginatedResult<ManagedRecipeListItem>>
{
    public async Task<PaginatedResult<ManagedRecipeListItem>> Handle(MyRecipesQuery request, CancellationToken ct)
    {
        if (!user.IsAuthenticated || string.IsNullOrEmpty(user.UserId)) throw new UnauthorizedException("Bạn cần đăng nhập.");
        if (request.ReviewQueue && !user.IsInRole("Admin")) throw new ForbiddenException("Chỉ Admin được xem hàng đợi duyệt.");
        var query = db.Recipes.AsNoTracking().Where(x => x.IsDeleted == request.Trash);
        if (request.ReviewQueue) query = query.Where(x => x.Status == RecipeStatus.PendingApproval && !x.IsDeleted);
        else query = query.Where(x => x.AuthorId == user.UserId);
        if (!request.ReviewQueue && request.Status is not null)
        {
            var status = Enum.Parse<RecipeStatus>(request.Status, true);
            query = query.Where(x => x.Status == status);
        }
        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new { x.Id, x.Title, x.Slug, x.Status, x.IsDeleted, x.AuthorId, x.CreatedAt, x.ReviewNote, x.RowVersion })
            .ToListAsync(ct);
        return new(rows.Select(x => new ManagedRecipeListItem(x.Id, x.Title, x.Slug, x.Status,
            x.IsDeleted, x.AuthorId, x.CreatedAt, x.ReviewNote, RecipeMappings.ToBytes(x.RowVersion))).ToList(),
            count, request.PageIndex, request.PageSize);
    }
}

public sealed class ManagedRecipeHandler(IApplicationDbContext db, IRecipeAuthorizationService authorization)
    : IRequestHandler<ManagedRecipeQuery, ManagedRecipeDto>
{
    public async Task<ManagedRecipeDto> Handle(ManagedRecipeQuery request, CancellationToken ct)
    {
        var recipe = await db.Recipes.AsNoTracking().Include(x => x.Steps).Include(x => x.Ingredients).Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct) ?? throw new NotFoundException("Không tìm thấy công thức.");
        await authorization.EnsureCanUpdateAsync(recipe, ct);
        return new(recipe.ToDto(), recipe.ReviewNote, recipe.Nutrition,
            recipe.Steps.Where(x => !x.IsDeleted).OrderBy(x => x.StepNumber).Select(x => new RecipeStepResponseDto(
                x.Id, x.RecipeId, x.StepNumber, x.Title, x.Description, x.TimerMinutes, x.ImageUrl)).ToArray(),
            recipe.Ingredients.Where(x => !x.IsDeleted).OrderBy(x => x.OrderIndex).Select(x => new RecipeIngredientResponseDto(
                x.Id, x.RecipeId, x.Name, x.Quantity, x.Unit, x.Notes, x.OrderIndex)).ToArray(),
            recipe.Images.Where(x => !x.IsDeleted).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.OrderIndex).Select(x => new RecipeImageResponseDto(
                x.Id, x.RecipeId, x.OriginalUrl, x.MediumUrl, x.ThumbnailUrl, x.IsPrimary, x.OrderIndex)).ToArray());
    }
}

public sealed class RecipeWorkflowHandler(IApplicationDbContext db, ICurrentUserService user,
    IRecipeAuthorizationService authorization, IRecipeCacheInvalidator cache)
    : IRequestHandler<RecipeWorkflowCommand>
{
    public async Task Handle(RecipeWorkflowCommand request, CancellationToken ct)
    {
        if (!user.IsAuthenticated) throw new UnauthorizedException("Bạn cần đăng nhập.");
        var recipe = await db.Recipes.Include(x => x.Steps).Include(x => x.Ingredients).Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct) ?? throw new NotFoundException("Không tìm thấy công thức.");
        if (request.Action is "publish" or "reject")
        {
            if (!user.IsInRole("Admin")) throw new ForbiddenException("Chỉ Admin được duyệt hoặc từ chối công thức.");
        }
        else await authorization.EnsureCanUpdateAsync(recipe, ct);
        RecipeEditing.CheckVersion(recipe, request.RowVersion);
        if (recipe.IsDeleted != (request.Action == "restore"))
            throw new ConflictException("RECIPE_TRASH_STATE", "Công thức không ở trạng thái phù hợp cho thao tác này.");

        void Require(RecipeStatus status)
        {
            if (recipe.Status != status) throw new ConflictException("RECIPE_STATUS_TRANSITION_INVALID", "Trạng thái công thức đã thay đổi. Vui lòng tải lại.");
        }
        switch (request.Action)
        {
            case "submit":
                Require(RecipeStatus.Draft);
                recipe.EnsureReadyForReview();
                recipe.ChangeStatus(RecipeStatus.PendingApproval);
                recipe.ReviewNote = null;
                break;
            case "withdraw":
                Require(RecipeStatus.PendingApproval);
                recipe.ChangeStatus(RecipeStatus.Draft);
                break;
            case "publish":
                Require(RecipeStatus.PendingApproval);
                recipe.Publish();
                break;
            case "reject":
                Require(RecipeStatus.PendingApproval);
                if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000)
                    throw new UnprocessableEntityException("REVIEW_REASON_REQUIRED", "Nhập lý do từ chối từ 1 đến 1000 ký tự.");
                recipe.ChangeStatus(RecipeStatus.Draft);
                recipe.ReviewNote = request.Reason.Trim();
                break;
            case "unpublish": Require(RecipeStatus.Published); recipe.ChangeStatus(RecipeStatus.Draft); break;
            case "archive": Require(RecipeStatus.Published); recipe.ChangeStatus(RecipeStatus.Archived); break;
            case "unarchive": Require(RecipeStatus.Archived); recipe.ChangeStatus(RecipeStatus.Draft); break;
            case "delete": recipe.SoftDelete(); break;
            case "restore": recipe.RestoreDraft(); break;
            default: throw new UnprocessableEntityException("RECIPE_ACTION_INVALID", "Thao tác không hợp lệ.");
        }
        recipe.UpdatedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new RecipeConcurrencyConflictException(); }
        await cache.InvalidateListAsync(ct);
        await cache.InvalidateDetailAsync(recipe.Slug, ct);
    }
}
