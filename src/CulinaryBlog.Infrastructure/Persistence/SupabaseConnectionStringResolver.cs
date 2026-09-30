using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CulinaryBlog.Infrastructure.Persistence;

public static class SupabaseConnectionStringResolver
{
    private const string DefaultPoolerRegion = "ap-southeast-1";

    public static string Resolve(IConfiguration configuration)
    {
        var explicitPooler = Normalize(
            configuration["SUPABASE_POOLER_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_POOLER_CONNECTION_STRING"));
        if (!string.IsNullOrWhiteSpace(explicitPooler))
        {
            return explicitPooler;
        }

        var configuredConnection = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["SUPABASE_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");

        return Resolve(
            configuredConnection,
            configuration["SUPABASE_POOLER_REGION"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_POOLER_REGION"));
    }

    public static string ResolveFromEnvironment()
    {
        var explicitPooler = Normalize(
            Environment.GetEnvironmentVariable("SUPABASE_POOLER_CONNECTION_STRING"));
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
        connectionString = Normalize(connectionString);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Chuỗi kết nối CSDL Supabase chưa được cấu hình. " +
                "Hãy đặt DefaultConnection, SUPABASE_CONNECTION_STRING hoặc SUPABASE_POOLER_CONNECTION_STRING.");
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

        var projectReference = host[directHostPrefix.Length..^directHostSuffix.Length];
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

    private static string? Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var normalized = connectionString.Trim().Trim('"', '\'');
        const string prefix = "SUPABASE_CONNECTION_STRING=";
        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[prefix.Length..].Trim().Trim('"', '\'');
        }

        return normalized;
    }
}
