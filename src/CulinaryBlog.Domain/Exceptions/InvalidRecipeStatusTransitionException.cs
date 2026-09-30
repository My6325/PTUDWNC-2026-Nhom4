using CulinaryBlog.Domain.Enums;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class InvalidRecipeStatusTransitionException(
    RecipeStatus currentStatus,
    RecipeStatus targetStatus)
    : RecipeDomainException(
        $"Cannot change recipe status from {currentStatus} to {targetStatus}.")
{
    public RecipeStatus CurrentStatus { get; } = currentStatus;

    public RecipeStatus TargetStatus { get; } = targetStatus;
}
