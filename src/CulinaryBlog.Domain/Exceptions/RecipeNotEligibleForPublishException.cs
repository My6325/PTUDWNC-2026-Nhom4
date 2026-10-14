namespace CulinaryBlog.Domain.Exceptions;

public sealed class RecipeNotEligibleForPublishException()
    : DomainException("Recipe must contain at least one step, one ingredient, and a primary image before it can be published.")
{
    public override string Code => "RECIPE_PUBLISH_INCOMPLETE";
}
