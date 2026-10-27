using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace CulinaryBlog.API.Middlewares;

/// <summary>
/// Middleware truy vết định danh yêu cầu xuyên suốt hệ thống (FR-OBS-002).
/// Đọc mã X-Correlation-ID từ HTTP Request hoặc tự sinh GUID mới,
/// gắn vào Response Header và đẩy vào LogContext của Serilog.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Đọc mã CorrelationId từ Request Header gửi lên hoặc tự sinh GUID mới
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        // 2. Gắn CorrelationId vào Response Header để Client/Trình duyệt đối soát khi gặp lỗi
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // 3. Đẩy CorrelationId vào LogContext của Serilog để mọi log trong luồng xử lý này đều được đóng dấu
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
