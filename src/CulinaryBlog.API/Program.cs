using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence.Seeders;

// 1. Tự động tìm và nạp biến môi trường từ file .env ở thư mục gốc dự án
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

var builder = WebApplication.CreateBuilder(args);

// Đăng ký các dịch vụ tầng Infrastructure (DbContext, Identity Core, JWT)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Tự động kiểm tra và nạp dữ liệu mẫu (Seeder) khi khởi động server
await CulinaryBlogSeeder.SeedAsync(app.Services);

app.MapGet("/", () => "Culinary Blog API is running!");

app.Run();
