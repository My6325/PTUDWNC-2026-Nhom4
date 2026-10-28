using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Features.Auth.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        ICurrentUserService currentUserService,
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _currentUserService = currentUserService;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException("Chưa xác thực người dùng.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("Người dùng không tồn tại hoặc tài khoản đã bị khóa.");
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            command.Request.CurrentPassword,
            command.Request.NewPassword);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code.Contains("PasswordMismatch", StringComparison.OrdinalIgnoreCase)))
            {
                throw new UnprocessableEntityException("INVALID_CURRENT_PASSWORD", "Mật khẩu hiện tại không chính xác.");
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new UnprocessableEntityException("PASSWORD_CHANGE_FAILED", $"Đổi mật khẩu thất bại: {errors}");
        }

        // Cập nhật SecurityStamp để làm mới trạng thái xác thực
        await _userManager.UpdateSecurityStampAsync(user);

        // Thu hồi toàn bộ Refresh Tokens cũ của user để đảm bảo an toàn tối đa
        try
        {
            var oldTokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in oldTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = "PasswordChange";
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể thu hồi các refresh tokens cũ sau khi đổi mật khẩu cho user {UserId}", user.Id);
        }

        _logger.LogInformation("Người dùng {UserId} ({Email}) đã đổi mật khẩu thành công.", user.Id, user.Email);
    }
}
