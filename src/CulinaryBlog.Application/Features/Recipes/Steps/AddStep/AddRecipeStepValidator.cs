using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Steps.AddStep;

public sealed class AddRecipeStepValidator : AbstractValidator<AddRecipeStepCommand>
{
    public AddRecipeStepValidator()
    {
        RuleFor(x => x.RecipeId)
            .NotEmpty()
            .WithMessage("RecipeId không được để trống.");

        RuleFor(x => x.Request.Title)
            .NotEmpty()
            .WithMessage("Tiêu đề bước làm không được để trống.")
            .MaximumLength(200)
            .WithMessage("Tiêu đề bước làm không được vượt quá 200 ký tự.");

        RuleFor(x => x.Request.Description)
            .NotEmpty()
            .WithMessage("Mô tả chi tiết bước làm không được để trống.");

        RuleFor(x => x.Request.TimerMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Request.TimerMinutes.HasValue)
            .WithMessage("Thời gian hẹn giờ phải lớn hơn hoặc bằng 0.");
    }
}
