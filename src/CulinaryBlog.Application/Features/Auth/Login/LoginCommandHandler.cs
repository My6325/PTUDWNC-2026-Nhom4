using Microsoft.Extensions.Logging;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Exceptions;

namespace CulinaryBlog.Application.Features.Auth.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IClientContext _clientContext;
    private readonly ILogger<LoginCommandHandler>? _logger;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IClientContext clientContext,
        ILogger<LoginCommandHandler>? logger = null)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _clientContext = clientContext;
        _logger = logger;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        // 1. Tìm user
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            _logger?.LogWarning("Đăng nhập thất bại: Không tìm thấy tài khoản với email {Email} từ IP {IpAddress}", request.Email, _clientContext.IpAddress);
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng");
        }

        // 2. Kiểm tra khóa tài khoản
        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger?.LogWarning("Đăng nhập bị chặn: Tài khoản {Email} ({UserId}) đang bị tạm khóa 15 phút từ IP {IpAddress}", user.Email, user.Id, _clientContext.IpAddress);
            throw new UserAccountLockedException("Tài khoản tạm khóa do đăng nhập sai quá 5 lần, thử lại sau 15 phút");
        }

        // 3. Kiểm tra mật khẩu
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            _logger?.LogWarning("Đăng nhập thất bại: Sai mật khẩu cho tài khoản {Email} ({UserId}) từ IP {IpAddress}", user.Email, user.Id, _clientContext.IpAddress);
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng");
        }

        if (!user.IsActive)
        {
            _logger?.LogWarning("Đăng nhập bị từ chối: Tài khoản {Email} ({UserId}) đã bị vô hiệu hóa từ IP {IpAddress}", user.Email, user.Id, _clientContext.IpAddress);
            throw new UserAccountLockedException("Tài khoản đã bị vô hiệu hóa.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        // 4. Lấy roles
        var roles = await _userManager.GetRolesAsync(user);

        // 5. Sinh tokens
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        
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

        _logger?.LogInformation("Người dùng {Email} ({UserId}) đã đăng nhập thành công với vai trò [{Roles}] từ IP {IpAddress}", user.Email, user.Id, string.Join(", ", roles), _clientContext.IpAddress);

        // 6. Trả về AuthResponseDto
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
