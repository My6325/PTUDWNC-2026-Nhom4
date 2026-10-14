using System.Text.Json;
using CulinaryBlog.API.ExceptionHandling;
using CulinaryBlog.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class StorageExceptionHandlingTests
{
    [Fact]
    public async Task StorageFailure_ShouldReturn502ProblemDetails()
    {
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        Assert.True(await handler.TryHandleAsync(context,
            new StorageUnavailableException("Không thể kết nối đến Supabase Storage."), CancellationToken.None));
        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        Assert.Equal(502, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("STORAGE_UNAVAILABLE", document.RootElement.GetProperty("code").GetString());
    }
}
