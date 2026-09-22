using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Persistence.Seeders;

public static class UserSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Cần truyền ILoggerFactory nếu muốn log chi tiết bằng ILogger, 
        // nhưng ở đây ta có thể dùng Console như CulinaryBlogSeeder
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==> [UserSeeder] Đang kiểm tra và nạp Roles, Users mẫu...");
        Console.ResetColor();

        // 1. Tạo Roles
        string[] roles = { "Admin", "Author", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Tạo Admin User
        var adminEmail = "admin@culinary.local";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                Id = "11111111-1111-1111-1111-111111111101", // Fix ID cho dễ quản lý (không bắt buộc nhưng tiện)
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "Quản trị viên",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("==> [UserSeeder] Đã tạo thành công tài khoản Admin.");
            }
        }

        // 3. Tạo Author User (Chef)
        var authorEmail = "chef_admin@culinary.local";
        if (await userManager.FindByEmailAsync(authorEmail) == null)
        {
            var authorUser = new ApplicationUser
            {
                Id = "22222222-2222-2222-2222-222222222201", // Bắt buộc ID này để TV3 dùng cho RecipeSeeder
                UserName = authorEmail,
                Email = authorEmail,
                DisplayName = "Đầu bếp trưởng",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(authorUser, "Author@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(authorUser, "Author");
                Console.WriteLine("==> [UserSeeder] Đã tạo thành công tài khoản Author.");
            }
        }
    }
}
