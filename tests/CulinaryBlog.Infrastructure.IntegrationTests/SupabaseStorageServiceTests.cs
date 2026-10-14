using System.Net;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class SupabaseStorageServiceTests
{
    private const string PublicUrl = "https://example.supabase.co/storage/v1/object/public/culinary-blog/recipes/test/image.png";

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task StorageHttpFailure_ShouldThrowForUploadAndDelete(HttpStatusCode status)
    {
        var handler = new StubHttpHandler(status);
        var service = CreateService(handler);
        using var stream = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<StorageUnavailableException>(() =>
            service.UploadFileAsync(stream, "recipes/test/image.png", "image/png"));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => service.DeleteFileAsync(PublicUrl));
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task UploadSuccess_ShouldReturnPublicUrlAndUseConfiguredBucket()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK);
        var service = CreateService(handler);
        using var stream = new MemoryStream([1, 2, 3]);
        Assert.Equal(PublicUrl, await service.UploadFileAsync(stream, "recipes/test/image.png", "image/png"));
        Assert.Equal("https://example.supabase.co/storage/v1/object/culinary-blog/recipes/test/image.png", handler.LastUrl);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
    }

    [Fact]
    public async Task DeleteMissingFile_ShouldAllowRetry()
    {
        var handler = new StubHttpHandler(HttpStatusCode.NotFound);
        await CreateService(handler).DeleteFileAsync(PublicUrl);
        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
    }

    [Fact]
    public async Task MissingConfiguration_ShouldThrowWithoutHttpRequest()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK);
        var service = CreateService(handler, configured: false);
        using var stream = new MemoryStream([1]);
        await Assert.ThrowsAsync<StorageUnavailableException>(() => service.UploadFileAsync(stream, "image.png", "image/png"));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => service.DeleteFileAsync(PublicUrl));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData("../image.png")]
    [InlineData("recipes/../../image.png")]
    [InlineData("recipes/%2e%2e/image.png")]
    [InlineData("recipes\\image.png")]
    [InlineData("/image.png")]
    public async Task UnsafeUploadPath_ShouldBeRejectedBeforeSending(string path)
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK);
        using var stream = new MemoryStream([1]);
        await Assert.ThrowsAsync<StorageUnavailableException>(() => CreateService(handler).UploadFileAsync(stream, path, "image/png"));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData("https://other.supabase.co/storage/v1/object/public/culinary-blog/image.png")]
    [InlineData("https://example.supabase.co/storage/v1/object/public/other-bucket/image.png")]
    [InlineData("https://example.supabase.co/storage/v1/object/public/culinary-blog/../image.png")]
    [InlineData("https://example.supabase.co/storage/v1/object/public/culinary-blog/%2e%2e/image.png")]
    public async Task UnsafeDeleteUrl_ShouldBeRejectedBeforeSending(string url)
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK);
        await Assert.ThrowsAsync<StorageUnavailableException>(() => CreateService(handler).DeleteFileAsync(url));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ConnectionFailure_ShouldBeReportedAsStorageFailure()
    {
        var service = CreateService(new StubHttpHandler(HttpStatusCode.OK) { Error = new HttpRequestException("Connection failed") });
        await Assert.ThrowsAsync<StorageUnavailableException>(() => service.DeleteFileAsync(PublicUrl));
    }

    [Fact]
    public async Task CallerCancellation_ShouldRemainCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = CreateService(new StubHttpHandler(HttpStatusCode.OK));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.DeleteFileAsync(PublicUrl, cancellation.Token));
    }

    private static SupabaseStorageService CreateService(StubHttpHandler handler, bool configured = true)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SUPABASE_URL"] = configured ? "https://example.supabase.co" : "",
            ["SUPABASE_SECRET_KEY"] = configured ? "test-key" : ""
        }).Build();
        return new SupabaseStorageService(new HttpClient(handler), config, NullLogger<SupabaseStorageService>.Instance);
    }

    private sealed class StubHttpHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? LastUrl { get; private set; }
        public HttpMethod? LastMethod { get; private set; }
        public Exception? Error { get; init; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            LastUrl = request.RequestUri!.AbsoluteUri;
            LastMethod = request.Method;
            return Error is null
                ? Task.FromResult(new HttpResponseMessage(status))
                : Task.FromException<HttpResponseMessage>(Error);
        }
    }
}
