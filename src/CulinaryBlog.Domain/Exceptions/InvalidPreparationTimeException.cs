namespace CulinaryBlog.Domain.Exceptions;

public sealed class InvalidPreparationTimeException(string timeType, int value)
    : RecipeDomainException($"Recipe {timeType} time must not be negative. Received: {value}.")
{
    public string TimeType { get; } = timeType;

    public int Value { get; } = value;
}
