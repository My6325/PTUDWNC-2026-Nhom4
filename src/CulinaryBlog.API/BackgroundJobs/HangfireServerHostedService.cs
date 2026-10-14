using Hangfire;

namespace CulinaryBlog.API.BackgroundJobs;

public sealed class HangfireServerHostedService : IHostedService
{
    private BackgroundJobServer? _server;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server = new BackgroundJobServer();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Dispose();
        _server = null;
        return Task.CompletedTask;
    }
}
