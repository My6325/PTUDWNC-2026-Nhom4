using Hangfire;

namespace CulinaryBlog.API.BackgroundJobs;

public sealed class ScopedHangfireJobActivator(IServiceScopeFactory serviceScopeFactory) : JobActivator
{
    public override JobActivatorScope BeginScope(JobActivatorContext context) =>
        new ServiceProviderJobActivatorScope(serviceScopeFactory.CreateScope());

    private sealed class ServiceProviderJobActivatorScope(IServiceScope scope) : JobActivatorScope
    {
        public override object Resolve(Type type) => scope.ServiceProvider.GetRequiredService(type);

        public override void DisposeScope() => scope.Dispose();
    }
}
