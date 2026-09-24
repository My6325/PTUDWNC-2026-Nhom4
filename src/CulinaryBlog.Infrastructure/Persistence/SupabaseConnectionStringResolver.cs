using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.Infrastructure.Persistence;

public static class SupabaseConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["SUPABASE_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Chuỗi kết nối CSDL Supabase (DefaultConnection hoặc SUPABASE_CONNECTION_STRING) chưa được cấu hình.");
        }

        connectionString = connectionString.Trim().Trim('"', '\'');
        if (connectionString.StartsWith("SUPABASE_CONNECTION_STRING=", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = connectionString.Substring("SUPABASE_CONNECTION_STRING=".Length).Trim().Trim('"', '\'');
        }

        return connectionString;
    }
}
