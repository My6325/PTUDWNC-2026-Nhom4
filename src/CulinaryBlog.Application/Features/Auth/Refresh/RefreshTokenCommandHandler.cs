using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using CulinaryBlog.Domain.Exceptions;
using CulinaryBlog.Application.Exceptions;

namespace CulinaryBlog.Application.Features.Auth.Refresh;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IClientContext _clientContext;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IClientContext clientContext)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _clientContext = clientContext;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var requestToken = command.Request.RefreshToken!;
        var hashedToken = _jwtService.HashToken(requestToken);

        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hashedToken, cancellationToken);

        if (tokenEntity == null)
        {
            throw new InvalidRefreshTokenException("Refresh token không hợp lệ.", "AUTH_REFRESH_TOKEN_INVALID");
        }

        if (tokenEntity.RevokedAt != null)
        {
            // Revoked: Check grace period
            var isGracePeriod = (DateTime.UtcNow - tokenEntity.RevokedAt.Value).TotalSeconds <= 30;
            var isSameIp = tokenEntity.RevokedByIp == _clientContext.IpAddress || tokenEntity.CreatedByIp == _clientContext.IpAddress;

            if (isGracePeriod && isSameIp)
            {
                // Grace period allowed, will continue to issue new token
            }
            else
            {
                // Reuse Detection: revoke all active tokens of the user
                var activeTokens = await _context.RefreshTokens
                    .Where(x => x.UserId == tokenEntity.UserId && x.RevokedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var activeToken in activeTokens)
                {
                    activeToken.RevokedAt = DateTime.UtcNow;
                    activeToken.RevokedByIp = _clientContext.IpAddress;
                }

                await _context.SaveChangesAsync(cancellationToken);
                throw new InvalidRefreshTokenException("Refresh token đã bị thu hồi.", "AUTH_REFRESH_TOKEN_REVOKED");
            }
        }

        if (tokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidRefreshTokenException("Refresh token đã hết hạn.", "AUTH_REFRESH_TOKEN_EXPIRED");
        }

        var user = await _userManager.FindByIdAsync(tokenEntity.UserId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("Người dùng không tồn tại hoặc đã bị vô hiệu hóa.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UserAccountLockedException("Tài khoản đang bị tạm khóa.");
        }

        // Token is valid, proceed with rotation
        tokenEntity.RevokedAt = DateTime.UtcNow;
        tokenEntity.RevokedByIp = _clientContext.IpAddress;

        var rawNewRefreshToken = _jwtService.GenerateRefreshToken();
        var hashedNewRefreshToken = _jwtService.HashToken(rawNewRefreshToken);

        tokenEntity.ReplacedByTokenHash = hashedNewRefreshToken;

        var newRefreshTokenEntity = new RefreshToken
        {
            TokenHash = hashedNewRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedByIp = _clientContext.IpAddress
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);

        // Sinh access token
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: rawNewRefreshToken,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            UserId: user.Id,
            Email: user.Email!,
            DisplayName: user.DisplayName,
            Roles: roles
        );
    }
}
