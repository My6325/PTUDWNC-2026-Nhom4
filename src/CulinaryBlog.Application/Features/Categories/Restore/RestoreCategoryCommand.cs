using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Categories.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Features.Categories.Restore;

/// <summary>
/// Command khôi phục danh mục ẩm thực từ Thùng rác (Soft Delete Undo).
/// </summary>
public sealed record RestoreCategoryCommand(Guid Id) : IRequest<CategoryDto>;

public sealed class RestoreCategoryValidator : AbstractValidator<RestoreCategoryCommand>
{
    public RestoreCategoryValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Id danh mục cần khôi phục không được để trống.");
    }
}

public sealed class RestoreCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache memoryCache)
    : IRequestHandler<RestoreCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(RestoreCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetDeletedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy danh mục trong thùng rác với Id '{command.Id}'.");

        // Khôi phục trạng thái hoạt động (Undo Soft Delete)
        category.IsDeleted = false;
        category.DeletedAt = null;
        category.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Vô hiệu hóa In-Memory Cache để danh sách ngoài public nạp lại danh mục này
        memoryCache.Remove(CategoryCacheKeys.All);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.OrderIndex,
            category.CreatedAt,
            category.UpdatedAt,
            category.DeletedAt
        );
    }
}
