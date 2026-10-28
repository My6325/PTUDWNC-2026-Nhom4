using System.Security.Claims;
using Asp.Versioning;
using Asp.Versioning.Builder;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.API.Endpoints;

public record RequestAuthorUpgradeRequest(string? Note);
public record ChangeUserRoleRequest(string TargetRole);
public record AdminUserItemDto(
    string Id,
    string UserName,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    bool IsActive,
    DateTime CreatedAt,
    IList<string> Roles,
    string? AuthorRequestStatus,
    string? AuthorRequestNote
);

/// <summary>
/// Đăng ký các endpoints Quản lý người dùng, Yêu cầu nâng cấp Đầu bếp và Phân quyền RBAC cho Admin.
/// </summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app, ApiVersionSet? versionSet = null)
    {
        var group = versionSet is not null
            ? app.MapGroup("/api/v{version:apiVersion}").WithApiVersionSet(versionSet).MapToApiVersion(new ApiVersion(1, 0))
            : app.MapGroup("/api/v1");

        // 1. Endpoint người dùng gửi yêu cầu trở thành Đầu bếp (Author Upgrade Request)
        group.MapPost("/users/me/request-author", async (
            [FromBody] RequestAuthorUpgradeRequest? request,
            ICurrentUserService currentUserService,
            UserManager<ApplicationUser> userManager) =>
        {
            var userId = currentUserService.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Any(r => r.Equals("Author", StringComparison.OrdinalIgnoreCase) || r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest(new { message = "Bạn đã có vai trò Đầu bếp hoặc Quản trị viên trong hệ thống." });
            }

            var existingClaims = await userManager.GetClaimsAsync(user);
            var pendingClaim = existingClaims.FirstOrDefault(c => c.Type == "AuthorRequest" && c.Value == "Pending");
            if (pendingClaim != null)
            {
                return Results.BadRequest(new { message = "Yêu cầu nâng cấp của bạn đang được Ban quản trị xem xét." });
            }

            // Xóa các claim cũ liên quan đến yêu cầu trước đó nếu có
            var oldStatusClaims = existingClaims.Where(c => c.Type == "AuthorRequest" || c.Type == "AuthorRequestNote" || c.Type == "AuthorRequestDate").ToList();
            if (oldStatusClaims.Count > 0)
            {
                await userManager.RemoveClaimsAsync(user, oldStatusClaims);
            }

            // Thêm các claim mới
            var newClaims = new List<Claim>
            {
                new("AuthorRequest", "Pending"),
                new("AuthorRequestDate", DateTime.UtcNow.ToString("o"))
            };

            if (!string.IsNullOrWhiteSpace(request?.Note))
            {
                newClaims.Add(new("AuthorRequestNote", request.Note.Trim()));
            }

            await userManager.AddClaimsAsync(user, newClaims);

            return Results.Ok(new { message = "Yêu cầu trở thành Đầu bếp đã được gửi thành công. Vui lòng chờ Ban quản trị xét duyệt!" });
        })
        .WithName("RequestAuthorUpgrade")
        .WithSummary("Gửi yêu cầu nâng cấp vai trò từ Độc giả lên Đầu bếp")
        .RequireAuthorization()
        .WithTags("Users")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(401);

        // 2. Endpoint Admin lấy danh sách toàn bộ người dùng & trạng thái vai trò
        group.MapGet("/admin/users", async (
            UserManager<ApplicationUser> userManager) =>
        {
            var users = await userManager.Users.ToListAsync();
            var userList = new List<AdminUserItemDto>();

            foreach (var u in users)
            {
                var roles = await userManager.GetRolesAsync(u);
                var claims = await userManager.GetClaimsAsync(u);

                var status = claims.FirstOrDefault(c => c.Type == "AuthorRequest")?.Value;
                var note = claims.FirstOrDefault(c => c.Type == "AuthorRequestNote")?.Value;

                userList.Add(new AdminUserItemDto(
                    Id: u.Id,
                    UserName: u.UserName ?? string.Empty,
                    Email: u.Email ?? string.Empty,
                    DisplayName: u.DisplayName,
                    AvatarUrl: u.AvatarUrl,
                    Bio: u.Bio,
                    IsActive: u.IsActive,
                    CreatedAt: u.CreatedAt,
                    Roles: roles,
                    AuthorRequestStatus: status,
                    AuthorRequestNote: note
                ));
            }

            // Ưu tiên hiển thị: Người đang chờ duyệt lên đầu, sau đó theo ngày tạo mới nhất
            var sortedUsers = userList
                .OrderByDescending(u => u.AuthorRequestStatus == "Pending")
                .ThenByDescending(u => u.CreatedAt)
                .ToList();

            return Results.Ok(sortedUsers);
        })
        .WithName("GetAdminUsers")
        .WithSummary("Lấy danh sách người dùng và trạng thái vai trò cho Admin")
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Admin")
        .Produces<List<AdminUserItemDto>>(200)
        .ProducesProblem(401)
        .ProducesProblem(403);

        // 3. Endpoint Admin chuyển đổi vai trò người dùng (Promote User <-> Author)
        group.MapPost("/admin/users/{id}/roles", async (
            string id,
            [FromBody] ChangeUserRoleRequest request,
            ICurrentUserService currentUserService,
            UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request?.TargetRole))
            {
                return Results.BadRequest(new { message = "Vai trò đích (TargetRole) không được để trống." });
            }

            var targetUser = await userManager.FindByIdAsync(id);
            if (targetUser == null)
            {
                return Results.NotFound(new { message = "Không tìm thấy người dùng với ID này." });
            }

            var currentRoles = await userManager.GetRolesAsync(targetUser);

            // Bảo vệ tài khoản Quản trị viên tối cao: Không thể tự hạ cấp hoặc hạ cấp Admin khác qua endpoint này
            if (currentRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest(new { message = "Không thể thay đổi vai trò của tài khoản Quản trị viên (Admin)." });
            }

            var claims = await userManager.GetClaimsAsync(targetUser);
            var reqClaims = claims.Where(c => c.Type == "AuthorRequest" || c.Type == "AuthorRequestNote").ToList();

            if (request.TargetRole.Equals("Author", StringComparison.OrdinalIgnoreCase))
            {
                if (!currentRoles.Contains("Author"))
                {
                    await userManager.AddToRoleAsync(targetUser, "Author");
                }

                // Cập nhật claim trạng thái duyệt thành Approved
                if (reqClaims.Count > 0)
                {
                    await userManager.RemoveClaimsAsync(targetUser, reqClaims);
                }
                await userManager.AddClaimAsync(targetUser, new Claim("AuthorRequest", "Approved"));

                var updatedRoles = await userManager.GetRolesAsync(targetUser);
                return Results.Ok(new
                {
                    message = $"Đã cấp vai trò Đầu bếp (Author) cho tài khoản {targetUser.DisplayName} thành công.",
                    roles = updatedRoles
                });
            }
            else if (request.TargetRole.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                if (currentRoles.Contains("Author"))
                {
                    await userManager.RemoveFromRoleAsync(targetUser, "Author");
                }

                if (!currentRoles.Contains("User"))
                {
                    await userManager.AddToRoleAsync(targetUser, "User");
                }

                // Xóa claim yêu cầu nếu có
                if (reqClaims.Count > 0)
                {
                    await userManager.RemoveClaimsAsync(targetUser, reqClaims);
                }

                var updatedRoles = await userManager.GetRolesAsync(targetUser);
                return Results.Ok(new
                {
                    message = $"Đã chuyển tài khoản {targetUser.DisplayName} về vai trò Độc giả (User) thành công.",
                    roles = updatedRoles
                });
            }
            else
            {
                return Results.BadRequest(new { message = "Vai trò đích không hợp lệ. Chỉ hỗ trợ 'Author' hoặc 'User'." });
            }
        })
        .WithName("ChangeUserRole")
        .WithSummary("Chuyển đổi vai trò người dùng giữa Độc giả và Đầu bếp")
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Admin")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(404);

        return app;
    }
}
