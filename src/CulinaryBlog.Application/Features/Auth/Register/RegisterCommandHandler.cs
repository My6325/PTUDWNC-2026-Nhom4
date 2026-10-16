using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using CulinaryBlog.Application.Exceptions;

using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Features.Auth.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IClientContext _clientContext;
    private readonly IBackgroundJobService? _backgroundJobService;
    private readonly ILogger<RegisterCommandHandler>? _logger;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IClientContext clientContext,
        IBackgroundJobService? backgroundJobService = null,
        ILogger<RegisterCommandHandler>? logger = null)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _clientContext = clientContext;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // 1. Kiểm tra email đã tồn tại chưa
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ConflictException("EMAIL_ALREADY_EXISTS", "Email đã được sử dụng");
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
            throw new UnprocessableEntityException("IDENTITY_CREATE_FAILED", $"Lỗi tạo tài khoản: {errors}");
        }

        // 4. Thêm role Author (role mặc định)
        var roleResult = await _userManager.AddToRoleAsync(user, "Author");
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new UnprocessableEntityException("IDENTITY_ROLE_FAILED", $"Lỗi gán vai trò: {errors}");
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
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedByIp = _clientContext.IpAddress
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Enqueue WelcomeEmailJob sau khi tạo tài khoản thành công
        try
        {
            _backgroundJobService?.EnqueueWelcomeEmail(user.Id);
        }
        catch (Exception ex)
        {
            // Lỗi gửi email hoặc enqueue tuyệt đối không làm gián đoạn hoặc thất bại kết quả đăng ký
            // Không ghi mật khẩu, token hay secrets vào log
            _logger?.LogWarning(ex, "Không thể đưa email chào mừng vào hàng đợi cho người dùng mới.");
        }

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
