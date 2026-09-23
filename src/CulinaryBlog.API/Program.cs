using CulinaryBlog.API.Endpoints;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

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

// 2. Đăng ký các dịch vụ tầng Application (MediatR, FluentValidation)
builder.Services.AddApplication();

// 3. Đăng ký các dịch vụ tầng Infrastructure (DbContext, Identity Core, JWT, Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// 4. Đăng ký Output Cache cho Recipes
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("RecipesCache", policy => policy
        .Expire(TimeSpan.FromMinutes(15))
        .SetVaryByQuery("*")
        .Tag("recipes"));
});

var app = builder.Build();

// 5. Tự động áp dụng migration trước khi seed để bảo đảm schema đã tồn tại.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}

// 6. Cấu hình trang test frontend tĩnh trong môi trường Development
if (app.Environment.IsDevelopment())
{
    var testFrontendPath = Path.GetFullPath(Path.Combine(
        app.Environment.ContentRootPath,
        "..",
        "..",
        "tests",
        "RecipeTestFrontend"));

    if (Directory.Exists(testFrontendPath))
    {
        var testFrontend = new PhysicalFileProvider(testFrontendPath);
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = testFrontend,
            RequestPath = "/recipe-test"
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = testFrontend,
            RequestPath = "/recipe-test"
        });
    }
}

app.UseOutputCache();

// 7. Tự động kiểm tra và nạp dữ liệu mẫu (Seeder) khi khởi động server
await CulinaryBlogSeeder.SeedAsync(app.Services);

// 8. Đăng ký các endpoints nghiệp vụ
app.MapGet("/", () => "Culinary Blog API is running!");
app.MapRecipeEndpoints();
app.MapAuthEndpoints();

app.Run();
