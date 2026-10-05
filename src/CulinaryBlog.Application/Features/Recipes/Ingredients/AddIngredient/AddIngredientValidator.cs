using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Ingredients.AddIngredient;

public sealed class AddIngredientValidator : AbstractValidator<AddIngredientCommand>
{
    public AddIngredientValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty()
            .WithMessage("RecipeId không được để trống.");

        RuleFor(x => x.Request.Name)
            .NotEmpty()
            .WithMessage("Tên nguyên liệu không được để trống.")
            .MaximumLength(100)
            .WithMessage("Tên nguyên liệu không được vượt quá 100 ký tự.");

        RuleFor(x => x.Request.Quantity)
            .GreaterThan(0)
            .When(x => x.Request.Quantity.HasValue)
            .WithMessage("Số lượng nguyên liệu phải lớn hơn 0.");

        RuleFor(x => x.Request.Unit)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Request.Unit))
            .WithMessage("Đơn vị tính không được vượt quá 50 ký tự.");
    }
}
