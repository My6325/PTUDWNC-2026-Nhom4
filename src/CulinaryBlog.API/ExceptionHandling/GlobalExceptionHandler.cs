using CulinaryBlog.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.ExceptionHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            ValidationException => (StatusCodes.Status422UnprocessableEntity, "Dữ liệu không hợp lệ", "VALIDATION_ERROR"),
            UnprocessableEntityException unprocessable =>
                (StatusCodes.Status422UnprocessableEntity, "Không thể xử lý dữ liệu", unprocessable.Code),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Chưa xác thực", "UNAUTHORIZED"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Không có quyền", "FORBIDDEN"),
            NotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy", "NOT_FOUND"),
            ConflictException conflict => (StatusCodes.Status409Conflict, "Xung đột dữ liệu", conflict.Code),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống", "INTERNAL_SERVER_ERROR")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError
                ? "Đã xảy ra lỗi không mong muốn."
                : exception.Message,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = code;

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray());
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
