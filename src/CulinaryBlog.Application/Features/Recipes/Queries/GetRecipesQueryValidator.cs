using CulinaryBlog.Domain.Enums;
using FluentValidation;

namespace CulinaryBlog.Application.Features.Recipes.Queries;

public sealed class GetRecipesQueryValidator : AbstractValidator<GetRecipesQuery>
{
    public GetRecipesQueryValidator()
    {
        RuleFor(query => query.PageIndex)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 50);

        RuleFor(query => query.Difficulty)
            .Must(BeValidDifficulty)
            .When(query => !string.IsNullOrWhiteSpace(query.Difficulty))
            .WithMessage("Difficulty must be Easy, Medium, or Hard.");

        RuleFor(query => query.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy)
                || string.Equals(sortBy.Trim(), "newest", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortBy must be 'newest'.");
    }

    private static bool BeValidDifficulty(string? value)
    {
        return Enum.TryParse<RecipeDifficulty>(value, ignoreCase: true, out var difficulty)
            && Enum.IsDefined(difficulty);
    }
}
