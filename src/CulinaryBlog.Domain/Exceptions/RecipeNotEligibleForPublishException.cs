namespace CulinaryBlog.Domain.Exceptions;

public sealed class RecipeNotEligibleForPublishException()
    : DomainException("Recipe must contain at least one step and one ingredient before it can be published.")
{
    public override string Code => "RECIPE_NOT_ELIGIBLE_FOR_PUBLISH";
}
