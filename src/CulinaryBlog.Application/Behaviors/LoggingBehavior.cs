using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>
/// Pipeline Behavior giám sát hiệu năng thực thi của các Use Cases (Commands & Queries).
/// Tự động bấm giờ bằng Stopwatch: Nếu thời gian xử lý vượt quá 500ms, tự động ghi log cảnh báo mức Warning (FR-OBS-002).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowThresholdMilliseconds = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (elapsedMs > SlowThresholdMilliseconds)
            {
                logger.LogWarning(
                    "⚠️ [CẢNH BÁO HIỆU NĂNG] Thao tác {RequestName} xử lý mất {ElapsedMs}ms (Vượt ngưỡng tiêu chuẩn {Threshold}ms)",
                    requestName,
                    elapsedMs,
                    SlowThresholdMilliseconds);
            }
        }
    }
}
