using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Application.Features.Auth.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // 1. Kiểm tra email đã tồn tại chưa
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            // TODO: sẽ thay bằng custom exception + middleware xử lý lỗi ở bước sau
            throw new InvalidOperationException("Email đã được sử dụng");
        }

        // 2. Tạo ApplicationUser mới
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.UserName,
            DisplayName = request.DisplayName,
            EmailConfirmed = false
        };

        // 3. Thực thi tạo user trong db
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Lỗi tạo tài khoản: {errors}");
        }

        // 4. Thêm role Author (role mặc định)
        var roleResult = await _userManager.AddToRoleAsync(user, "Author");
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Lỗi gán vai trò: {errors}");
        }

        // 5. Sinh accessToken
        var roles = new List<string> { "Author" };
        var accessToken = _jwtService.GenerateAccessToken(user, roles);

        // 6. Sinh refreshToken và lưu hash vào DB
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var hashedRefreshToken = _jwtService.HashToken(rawRefreshToken);
        
        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = hashedRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        // 7. TODO(FR-JOB-001): enqueue WelcomeEmailJob qua Hangfire khi module Hangfire được thiết lập

        // 8. Trả về AuthResponseDto
        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            UserId: user.Id,
            Email: user.Email!,
            DisplayName: user.DisplayName,
            Roles: roles
        );
    }
}
