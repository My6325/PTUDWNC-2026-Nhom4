using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Categories.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Features.Categories.Update;

public sealed record UpdateCategoryRequest(
    string? Name = null,
    string? Description = null,
    string? ImageUrl = null,
    int? OrderIndex = null);

public sealed record UpdateCategoryCommand(Guid Id, UpdateCategoryRequest Request) : IRequest<CategoryDto>;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Id danh mục không được để trống.");

        RuleFor(command => command.Request.Name)
            .Must(name => name is null || name.Trim().Length is >= 2 and <= 100)
            .WithMessage("Tên danh mục phải có độ dài từ 2 đến 100 ký tự.")
            .Must(name => name is null || (!name.Contains('<') && !name.Contains('>')))
            .WithMessage("Tên danh mục không được chứa thẻ HTML.");

        RuleFor(command => command.Request.OrderIndex)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Thứ tự hiển thị (OrderIndex) phải lớn hơn hoặc bằng 0.");
    }
}

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache memoryCache)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy danh mục với Id '{command.Id}'.");

        var request = command.Request;
        var newName = request.Name?.Trim() ?? category.Name;
        if (!string.Equals(category.Name, newName, StringComparison.OrdinalIgnoreCase) &&
            await categoryRepository.NameExistsAsync(newName, cancellationToken, category.Id))
        {
            throw new ConflictException("CATEGORY_NAME_CONFLICT", "Tên danh mục đã tồn tại.");
        }

        category.Name = newName;
        if (request.Description is not null)
            category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.ImageUrl is not null)
            category.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        category.OrderIndex = request.OrderIndex ?? category.OrderIndex;

        // Xác thực quy tắc nghiệp vụ Domain
        category.Validate();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Vô hiệu hóa In-Memory Cache để các request sau nạp dữ liệu mới
        memoryCache.Remove(CategoryCacheKeys.All);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.OrderIndex,
            category.CreatedAt,
            category.UpdatedAt);
    }
}
