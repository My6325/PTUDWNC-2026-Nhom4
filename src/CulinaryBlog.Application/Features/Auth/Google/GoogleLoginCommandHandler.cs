using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CulinaryBlog.Application.Features.Auth.Google;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly IClientContext _clientContext;

    public GoogleLoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IGoogleTokenValidator googleTokenValidator,
        IApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings,
        IClientContext clientContext)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _googleTokenValidator = googleTokenValidator;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _clientContext = clientContext;
    }

    public async Task<AuthResponseDto> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var payload = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (payload == null)
        {
            throw new UnauthorizedException("Invalid Google token.");
        }

        var user = await _userManager.FindByEmailAsync(payload.Email);

        if (user != null)
        {
            if (!user.IsActive)
            {
                throw new ForbiddenException("Account is locked or inactive.");
            }
        }
        else
        {
            user = new ApplicationUser
            {
                UserName = payload.Email.Split('@')[0] + "_" + Guid.NewGuid().ToString("N").Substring(0, 5),
                Email = payload.Email,
                DisplayName = string.IsNullOrEmpty(payload.Name) ? payload.Email.Split('@')[0] : payload.Name,
                AvatarUrl = payload.PictureUrl,
                EmailConfirmed = true,
                IsActive = true
            };

            var password = GenerateRandomPassword();
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new System.Exception($"Failed to create user: {errors}");
            }

            await _userManager.AddToRoleAsync(user, "Author");
            await _userManager.AddClaimAsync(user, new Claim("LoginProvider", "Google"));
        }

        var roles = await _userManager.GetRolesAsync(user);

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

    private string GenerateRandomPassword()
    {
        return Guid.NewGuid().ToString("N") + "A1!";
    }
}
