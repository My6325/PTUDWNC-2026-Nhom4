using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Categories.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Features.Categories.Delete;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest;

public sealed class DeleteCategoryValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("Id danh mục không được để trống.");
    }
}

public sealed class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache memoryCache)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy danh mục với Id '{command.Id}'.");

        var recipeCount = await categoryRepository.CountRecipesAsync(category.Id, cancellationToken);
        if (recipeCount > 0)
        {
            throw new ConflictException("CATEGORY_HAS_RECIPES",
                $"Danh mục còn chứa {recipeCount} công thức. Hãy chuyển chúng sang danh mục khác trước khi xóa.");
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Vô hiệu hóa In-Memory Cache
        memoryCache.Remove(CategoryCacheKeys.All);
    }
}
