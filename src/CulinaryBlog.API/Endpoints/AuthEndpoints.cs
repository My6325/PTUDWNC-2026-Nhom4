using Asp.Versioning;
using Asp.Versioning.Builder;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using CulinaryBlog.Application.Features.Auth.Login;
using CulinaryBlog.Application.Features.Auth.Register;
using CulinaryBlog.Application.Features.Auth.Refresh;
using CulinaryBlog.Application.Features.Auth.Logout;
using CulinaryBlog.Application.Features.Auth.Google;
using CulinaryBlog.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CulinaryBlog.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, ApiVersionSet? versionSet = null)
    {
        var group = versionSet is not null
            ? app.MapGroup("/api/v{version:apiVersion}/auth").WithApiVersionSet(versionSet).MapToApiVersion(new ApiVersion(1, 0))
            : app.MapGroup("/api/v1/auth");

        group.WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, HttpResponse httpResponse, IOptions<JwtSettings> jwtOptions, IWebHostEnvironment env, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new RegisterCommand(request);
            var response = await sender.Send(command, cancellationToken);
            AppendRefreshTokenCookie(httpResponse, response.RefreshToken, jwtOptions.Value, env);
            return Results.Ok(response);
        })
        .WithName("Register")
        .WithSummary("Register a new user account")
        .AllowAnonymous()
        .RequireRateLimiting("auth-sensitive")
        .Produces<AuthResponseDto>(200)
        .ProducesProblem(409)
        .ProducesProblem(422)
        .ProducesProblem(429);

        group.MapPost("/login", async (LoginRequest request, HttpResponse httpResponse, IOptions<JwtSettings> jwtOptions, IWebHostEnvironment env, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request);
            var response = await sender.Send(command, cancellationToken);
            AppendRefreshTokenCookie(httpResponse, response.RefreshToken, jwtOptions.Value, env);
            return Results.Ok(response);
        })
        .WithName("Login")
        .WithSummary("Log in and obtain access and refresh tokens")
        .AllowAnonymous()
        .RequireRateLimiting("auth-sensitive")
        .Produces<AuthResponseDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(422)
        .ProducesProblem(423)
        .ProducesProblem(429);

        group.MapPost("/google", async (GoogleLoginRequest request, HttpContext context, HttpResponse httpResponse, IOptions<JwtSettings> jwtOptions, IWebHostEnvironment env, ISender sender, CancellationToken cancellationToken) =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var command = new GoogleLoginCommand(request.IdToken, clientIp, userAgent);
            var response = await sender.Send(command, cancellationToken);
            AppendRefreshTokenCookie(httpResponse, response.RefreshToken, jwtOptions.Value, env);
            return Results.Ok(response);
        })
        .WithName("GoogleLogin")
        .WithSummary("Log in using Google OAuth 2.0")
        .AllowAnonymous()
        .RequireRateLimiting("auth-sensitive")
        .Produces<AuthResponseDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(422)
        .ProducesProblem(429);

        group.MapPost("/refresh", async (HttpRequest httpRequest, HttpResponse httpResponse, IOptions<JwtSettings> jwtOptions, IWebHostEnvironment env, ISender sender, CancellationToken cancellationToken) =>
        {
            string? token = null;
            if (httpRequest.HasJsonContentType())
            {
                try
                {
                    var body = await httpRequest.ReadFromJsonAsync<RefreshTokenRequest>(cancellationToken);
                    token = body?.RefreshToken;
                }
                catch { }
            }
            if (string.IsNullOrEmpty(token))
            {
                token = httpRequest.Cookies["refreshToken"];
            }

            var command = new RefreshTokenCommand(new RefreshTokenRequest(token));
            var response = await sender.Send(command, cancellationToken);

            AppendRefreshTokenCookie(httpResponse, response.RefreshToken, jwtOptions.Value, env);

            return Results.Ok(response);
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh access and refresh tokens")
        .AllowAnonymous()
        .RequireRateLimiting("auth-sensitive")
        .Produces<AuthResponseDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(422)
        .ProducesProblem(429);

        group.MapPost("/logout", async (HttpRequest httpRequest, HttpResponse httpResponse, IWebHostEnvironment env, ISender sender, CancellationToken cancellationToken) =>
        {
            string? token = null;
            if (httpRequest.HasJsonContentType())
            {
                try
                {
                    var body = await httpRequest.ReadFromJsonAsync<RefreshTokenRequest>(cancellationToken);
                    token = body?.RefreshToken;
                }
                catch { }
            }
            if (string.IsNullOrEmpty(token))
            {
                token = httpRequest.Cookies["refreshToken"];
            }

            var command = new LogoutCommand(new RefreshTokenRequest(token));
            await sender.Send(command, cancellationToken);

            httpResponse.Cookies.Delete("refreshToken", new CookieOptions
            {
                Path = "/api/v1/auth",
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = !env.IsDevelopment()
            });
            return Results.NoContent();
        })
        .WithName("Logout")
        .WithSummary("Log out and revoke refresh token")
        .RequireAuthorization()
        .Produces(204)
        .ProducesProblem(401)
        .ProducesProblem(422);

        group.MapGet("/me", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var query = new CulinaryBlog.Application.Features.Auth.Me.GetCurrentUserProfileQuery();
            var response = await sender.Send(query, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get the current user's profile")
        .RequireAuthorization()
        .Produces<UserDto>(200)
        .ProducesProblem(401);

        group.MapPatch("/profile", async (CulinaryBlog.Application.Features.Auth.Profile.UpdateProfileRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new CulinaryBlog.Application.Features.Auth.Profile.UpdateProfileCommand(request);
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("UpdateProfile")
        .WithSummary("Update the current user's profile")
        .RequireAuthorization()
        .Produces<UserDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(422);

        group.MapPost("/profile/avatar", async (
            IFormFile file,
            ICurrentUserService currentUserService,
            UserManager<ApplicationUser> userManager,
            ISupabaseStorageService storageService,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("CulinaryBlog.API.Endpoints.AuthEndpoints");
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

            if (file == null || file.Length == 0)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "EMPTY_FILE",
                    detail: "Tệp tin hình ảnh không được để trống.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "FILE_TOO_LARGE",
                    detail: "Dung lượng ảnh đại diện không được vượt quá 5MB.");
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var bytes = memoryStream.ToArray();

            var (isValid, extension, detectedContentType) = ValidateAvatarMagicBytes(bytes);
            if (!isValid)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "INVALID_IMAGE_FORMAT",
                    detail: "Định dạng tệp không hợp lệ. Hệ thống chỉ chấp nhận hình ảnh JPEG, PNG hoặc WebP.");
            }

            var safeFileName = $"avatars/{user.Id}_{Guid.NewGuid()}{extension}";

            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.Contains("/culinary-blog/avatars/"))
            {
                try
                {
                    await storageService.DeleteFileAsync(user.AvatarUrl, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Không thể xóa ảnh đại diện cũ của User {UserId}", user.Id);
                }
            }

            memoryStream.Position = 0;
            var publicUrl = await storageService.UploadFileAsync(
                memoryStream,
                safeFileName,
                detectedContentType,
                cancellationToken);

            user.AvatarUrl = publicUrl;
            await userManager.UpdateAsync(user);

            var roles = await userManager.GetRolesAsync(user);
            logger.LogInformation("Người dùng {UserId} ({Email}) đã tải lên ảnh đại diện mới thành công: {AvatarUrl}", user.Id, user.Email, publicUrl);

            return Results.Ok(new
            {
                avatarUrl = publicUrl,
                message = "Cập nhật ảnh đại diện thành công.",
                user = new UserDto(
                    user.Id,
                    user.UserName ?? string.Empty,
                    user.Email ?? string.Empty,
                    user.DisplayName,
                    user.AvatarUrl,
                    user.Bio,
                    roles,
                    user.CreatedAt)
            });
        })
        .WithName("UploadAvatar")
        .WithSummary("Tải lên ảnh đại diện mới cho người dùng hiện tại")
        .RequireAuthorization()
        .DisableAntiforgery()
        .Produces(200)
        .ProducesProblem(401)
        .ProducesProblem(422);

        group.MapPost("/change-password", async (CulinaryBlog.Application.Features.Auth.ChangePassword.ChangePasswordRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new CulinaryBlog.Application.Features.Auth.ChangePassword.ChangePasswordCommand(request);
            await sender.Send(command, cancellationToken);
            return Results.Ok(new { message = "Đổi mật khẩu thành công." });
        })
        .WithName("ChangePassword")
        .WithSummary("Đổi mật khẩu cho người dùng hiện tại")
        .RequireAuthorization()
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(422);

        group.MapPost("/forgot-password", async (CulinaryBlog.Application.Features.Auth.ForgotPassword.ForgotPasswordRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new CulinaryBlog.Application.Features.Auth.ForgotPassword.ForgotPasswordCommand(request);
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("ForgotPassword")
        .WithSummary("Yêu cầu gửi liên kết đặt lại mật khẩu qua email")
        .AllowAnonymous()
        .RequireRateLimiting("auth-sensitive")
        .Produces<CulinaryBlog.Application.Features.Auth.ForgotPassword.ForgotPasswordResponseDto>(200)
        .ProducesProblem(422)
        .ProducesProblem(429);

        group.MapPost("/reset-password", async (CulinaryBlog.Application.Features.Auth.ResetPassword.ResetPasswordRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new CulinaryBlog.Application.Features.Auth.ResetPassword.ResetPasswordCommand(request);
            await sender.Send(command, cancellationToken);
            return Results.Ok(new { message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới." });
        })
        .WithName("ResetPassword")
        .WithSummary("Đặt lại mật khẩu bằng mã token")
        .AllowAnonymous()
        .RequireRateLimiting("auth-sensitive")
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(422)
        .ProducesProblem(429);

        return app;
    }

    private static (bool IsValid, string Extension, string ContentType) ValidateAvatarMagicBytes(byte[] bytes)
    {
        // JPEG: FF D8 FF
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return (true, ".jpg", "image/jpeg");
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return (true, ".png", "image/png");
        }

        // WebP: RIFF .... WEBP
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return (true, ".webp", "image/webp");
        }

        return (false, string.Empty, string.Empty);
    }

    private static void AppendRefreshTokenCookie(HttpResponse httpResponse, string refreshToken, JwtSettings jwtSettings, IWebHostEnvironment env)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Secure = !env.IsDevelopment(),
            Expires = DateTimeOffset.UtcNow.AddDays(jwtSettings.RefreshTokenExpirationDays)
        };
        httpResponse.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
