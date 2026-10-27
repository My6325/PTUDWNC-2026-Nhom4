using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Features.Auth.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null || !user.IsActive)
        {
            throw new UnprocessableEntityException("INVALID_RESET_TOKEN", "Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        var result = await _userManager.ResetPasswordAsync(
            user,
            command.Request.Token,
            command.Request.NewPassword);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Đặt lại mật khẩu thất bại cho user {Email}: {Errors}",
                email,
                string.Join(", ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));

            throw new UnprocessableEntityException("INVALID_RESET_TOKEN", "Mã đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        // Mở khóa tài khoản nếu đang bị khóa do nhập sai mật khẩu trước đó
        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }
        await _userManager.ResetAccessFailedCountAsync(user);

        // Làm mới SecurityStamp
        await _userManager.UpdateSecurityStampAsync(user);

        // Thu hồi toàn bộ Refresh Tokens cũ nhằm bảo vệ an toàn tối đa cho tài khoản
        try
        {
            var oldTokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in oldTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = "PasswordReset";
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể thu hồi các refresh tokens cũ sau khi đặt lại mật khẩu cho user {UserId}", user.Id);
        }

        _logger.LogInformation("Người dùng {UserId} ({Email}) đã đặt lại mật khẩu thành công.", user.Id, email);
    }
}
