using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CulinaryBlog.Infrastructure.Persistence;

public static class SupabaseConnectionStringResolver
{
    private const string DefaultPoolerRegion = "ap-southeast-1";

    public static string Resolve(IConfiguration configuration)
    {
        var explicitPooler = configuration["SUPABASE_POOLER_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_POOLER_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(explicitPooler))
        {
            return explicitPooler;
        }

        var configuredConnection = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["SUPABASE_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");

        return Resolve(configuredConnection, configuration["SUPABASE_POOLER_REGION"]);
    }

    public static string ResolveFromEnvironment()
    {
        var explicitPooler = Environment.GetEnvironmentVariable("SUPABASE_POOLER_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(explicitPooler))
        {
            return explicitPooler;
        }

        return Resolve(
            Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("SUPABASE_POOLER_REGION"));
    }

    public static string Resolve(string? connectionString, string? poolerRegion)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SUPABASE_CONNECTION_STRING hoặc SUPABASE_POOLER_CONNECTION_STRING chưa được cấu hình.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        const string directHostPrefix = "db.";
        const string directHostSuffix = ".supabase.co";

        var host = builder.Host;
        if (string.IsNullOrWhiteSpace(host) ||
            !host.StartsWith(directHostPrefix, StringComparison.OrdinalIgnoreCase) ||
            !host.EndsWith(directHostSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return builder.ConnectionString;
        }

        var projectReference = host[
            directHostPrefix.Length..^directHostSuffix.Length];
        var region = string.IsNullOrWhiteSpace(poolerRegion)
            ? DefaultPoolerRegion
            : poolerRegion.Trim();

        builder.Host = $"aws-0-{region}.pooler.supabase.com";
        builder.Port = 5432;
        if (string.Equals(builder.Username, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            builder.Username = $"postgres.{projectReference}";
        }

        var configuredSslMode = Environment.GetEnvironmentVariable("SUPABASE_POOLER_SSL_MODE");
        builder.SslMode = Enum.TryParse<SslMode>(configuredSslMode, true, out var sslMode)
            ? sslMode
            : SslMode.Disable;
        builder.Pooling = true;
        return builder.ConnectionString;
    }
}
