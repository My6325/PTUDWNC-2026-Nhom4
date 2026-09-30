using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>
/// Factory khởi tạo ApplicationDbContext ở thời điểm Design-Time (khi chạy lệnh dotnet ef CLI).
/// Giúp công cụ EF Core CLI khởi tạo DbContext trực tiếp mà không bị phụ thuộc vào các dịch vụ Web Host phức tạp.
/// Chuẩn kỹ thuật theo tài liệu Microsoft: https://go.microsoft.com/fwlink/?linkid=851728
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // 1. Tự động tìm và nạp file .env ở thư mục gốc nếu chưa có biến môi trường
        var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, ".env")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir != null)
        {
            var envFile = Path.Combine(currentDir.FullName, ".env");
            foreach (var line in File.ReadAllLines(envFile))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
                var parts = trimmed.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var val = parts[1].Trim().Trim('"', '\'');
                    if (val.StartsWith("SUPABASE_CONNECTION_STRING=", StringComparison.OrdinalIgnoreCase))
                    {
                        val = val.Substring("SUPABASE_CONNECTION_STRING=".Length).Trim().Trim('"', '\'');
                    }
                    Environment.SetEnvironmentVariable(key, val);
                }
            }
        }

        var connectionString = SupabaseConnectionStringResolver.ResolveFromEnvironment();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
