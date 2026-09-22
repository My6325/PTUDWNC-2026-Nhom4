using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Persistence.Seeders;

/// <summary>
/// Lớp điều phối nạp dữ liệu mẫu tập trung của toàn bộ hệ thống Culinary Blog.
/// Do Trưởng nhóm (TV1) quản trị và điều phối các Seeder của từng thành viên:
/// 1. TV2: UserSeeder (Tài khoản Admin & Author mẫu)
/// 2. TV1: CategorySeeder (>= 20 Danh mục ẩm thực)
/// 3. TV3: RecipeSeeder (>= 100 Recipes ngẫu nhiên Bogus)
/// </summary>
public static class CulinaryBlogSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<ApplicationDbContext>>();

        try
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=======================================================");
            Console.WriteLine("==> [Seeder] Bắt đầu kiểm tra và nạp dữ liệu mẫu...");
            Console.ResetColor();

            // 1. Nạp danh mục ẩm thực (TV1 - Trưởng nhóm phụ trách)
            await CategorySeeder.SeedAsync(context);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==> [Seeder] Đã nạp THÀNH CÔNG 25 danh mục vào bảng Categories!");
            Console.WriteLine("=======================================================\n");
            Console.ResetColor();

            // 2. Chỗ cắm nạp tài khoản người dùng mẫu (TV2 - Linh phụ trách UserSeeder)
            await UserSeeder.SeedAsync(scope.ServiceProvider);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==> [Seeder] Đã nạp THÀNH CÔNG tài khoản User mẫu!");
            Console.WriteLine("=======================================================\n");
            Console.ResetColor();

            // 3. Nạp 100+ công thức ngẫu nhiên Bogus (TV3 - Vương phụ trách RecipeSeeder)
            await RecipeSeeder.SeedAsync(context);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"==> [Seeder LỖI]: {ex.Message}");
            Console.ResetColor();
            logger?.LogError(ex, "==> [Seeder] Gặp lỗi trong quá trình nạp dữ liệu mẫu: {Message}", ex.Message);
            throw;
        }
    }
}
