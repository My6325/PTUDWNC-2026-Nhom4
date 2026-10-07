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
                || new[] { "newest", "cooktime", "relevance" }.Contains(sortBy.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be 'newest', 'cookTime', or 'relevance'.");

        RuleFor(query => query.MinCookTimeMinutes).GreaterThanOrEqualTo(0).When(query => query.MinCookTimeMinutes.HasValue);
        RuleFor(query => query.MaxCookTimeMinutes).GreaterThanOrEqualTo(0).When(query => query.MaxCookTimeMinutes.HasValue);
        RuleFor(query => query.MinServings).GreaterThan(0).When(query => query.MinServings.HasValue);
        RuleFor(query => query.MaxServings).GreaterThan(0).When(query => query.MaxServings.HasValue);
        RuleFor(query => query).Must(query => !query.MinCookTimeMinutes.HasValue || !query.MaxCookTimeMinutes.HasValue || query.MinCookTimeMinutes <= query.MaxCookTimeMinutes)
            .WithMessage("MinCookTimeMinutes must not exceed MaxCookTimeMinutes.");
        RuleFor(query => query).Must(query => !query.MinServings.HasValue || !query.MaxServings.HasValue || query.MinServings <= query.MaxServings)
            .WithMessage("MinServings must not exceed MaxServings.");
        RuleFor(query => query).Must(query => !string.Equals(query.SortBy?.Trim(), "relevance", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(query.SearchTerm))
            .WithMessage("SortBy 'relevance' requires SearchTerm.");
    }

    private static bool BeValidDifficulty(string? value)
    {
        return Enum.TryParse<RecipeDifficulty>(value, ignoreCase: true, out var difficulty)
            && Enum.IsDefined(difficulty);
    }
}
