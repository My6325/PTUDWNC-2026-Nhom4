using System.Net;
using System.Net.Http.Headers;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Services;

/// <summary>Quản lý tệp trong bucket culinary-blog; lỗi Storage phải được truyền về API.</summary>
public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SupabaseStorageService> _logger;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    private const string BucketName = "culinary-blog";

    public SupabaseStorageService(HttpClient httpClient, IConfiguration configuration,
        ILogger<SupabaseStorageService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _supabaseUrl = (configuration["SUPABASE_URL"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_URL") ?? string.Empty).TrimEnd('/');
        _supabaseKey = configuration["SUPABASE_SECRET_KEY"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_SECRET_KEY")
            ?? configuration["SUPABASE_PUBLISHABLE_KEY"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_PUBLISHABLE_KEY") ?? string.Empty;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var path = ValidateObjectPath(fileName);
        using var request = CreateRequest(HttpMethod.Post, path);
        // Do not overwrite an existing object; the caller generates a unique GUID.
        request.Headers.Add("x-upsert", "false");
        request.Content = new StreamContent(fileStream);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        await SendAsync(request, allowNotFound: false, cancellationToken);
        return $"{_supabaseUrl}/storage/v1/object/public/{BucketName}/{path}";
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var prefix = $"{_supabaseUrl}/storage/v1/object/public/{BucketName}/";
        if (!fileUrl.StartsWith(prefix, StringComparison.Ordinal) ||
            !Uri.TryCreate(fileUrl, UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new StorageUnavailableException("Đường dẫn tệp không thuộc bucket culinary-blog đã cấu hình.");
        }

        var path = ValidateObjectPath(fileUrl[prefix.Length..]);
        using var request = CreateRequest(HttpMethod.Delete, path);
        // A retry after a partial deletion must also succeed for already deleted files.
        await SendAsync(request, allowNotFound: true, cancellationToken);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"{_supabaseUrl}/storage/v1/object/{BucketName}/{path}");
        request.Headers.Add("apikey", _supabaseKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _supabaseKey);
        return request;
    }

    private async Task SendAsync(HttpRequestMessage request, bool allowNotFound, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode || (allowNotFound && response.StatusCode == HttpStatusCode.NotFound))
                return;

            _logger.LogWarning("Supabase Storage {Method} failed with status {StatusCode}", request.Method, response.StatusCode);
            throw new StorageUnavailableException("Không thể hoàn tất thao tác trên Supabase Storage.");
        }
        catch (HttpRequestException exception)
        {
            throw new StorageUnavailableException("Không thể kết nối đến Supabase Storage.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new StorageUnavailableException("Supabase Storage không phản hồi kịp thời.", exception);
        }
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_supabaseKey) ||
            !Uri.TryCreate(_supabaseUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new StorageUnavailableException("Supabase Storage chưa được cấu hình hợp lệ.");
        }
    }

    private static string ValidateObjectPath(string path)
    {
        // Generated object keys use only these characters. Reject encoded separators,
        // traversal segments and URI delimiters before HttpClient normalizes the URL.
        if (string.IsNullOrWhiteSpace(path) ||
            path.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '/' or '-' or '_' or '.')) ||
            path.Split('/').Any(segment => segment.Length == 0 || segment is "." or ".."))
        {
            throw new StorageUnavailableException("Đường dẫn tệp lưu trữ không hợp lệ.");
        }
        return path;
    }
}
