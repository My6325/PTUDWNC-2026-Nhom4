using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Categories.Common;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Features.Categories.Create;

public sealed record CreateCategoryRequest(string Name, string? Description);
public sealed record CreateCategoryCommand(CreateCategoryRequest Request) : IRequest<CategoryDto>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(command => command.Request.Name)
            .Must(name => name is not null && name.Trim().Length is >= 2 and <= 50)
            .WithMessage("Name phải có độ dài từ 2 đến 50 ký tự.")
            .Must(name => name is not null && !name.Contains('<') && !name.Contains('>'))
            .WithMessage("Name không được chứa thẻ HTML.");
    }
}

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IMemoryCache memoryCache)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var name = request.Name.Trim();
        var baseSlug = SlugHelper.GenerateSlug(name);
        if (string.IsNullOrEmpty(baseSlug))
        {
            throw new UnprocessableEntityException("CATEGORY_NAME_INVALID", "Name phải chứa ít nhất một chữ cái hoặc chữ số.");
        }

        var slug = baseSlug;
        var suffix = 2;
        while (await categoryRepository.SlugExistsAsync(slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        var category = new Category
        {
            Name = name,
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            OrderIndex = await categoryRepository.GetNextOrderIndexAsync(cancellationToken)
        };

        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        memoryCache.Remove(CategoryCacheKeys.All);

        return new CategoryDto(category.Id, category.Name, category.Slug, category.Description,
            category.ImageUrl, category.OrderIndex);
    }
}
