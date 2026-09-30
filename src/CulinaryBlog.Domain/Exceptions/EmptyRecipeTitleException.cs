namespace CulinaryBlog.Domain.Exceptions;

public sealed class EmptyRecipeTitleException()
    : RecipeDomainException("Recipe title must not be empty.");
