namespace CulinaryBlog.Application.Exceptions;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ForbiddenException(string message) : Exception(message);

public sealed class UnauthorizedException(string message) : Exception(message);

public class ConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class UnprocessableEntityException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class RecipeConcurrencyConflictException()
    : ConflictException(
        "RECIPE_CONCURRENCY_CONFLICT",
        "Công thức đã được người khác cập nhật. Vui lòng tải lại dữ liệu.");
