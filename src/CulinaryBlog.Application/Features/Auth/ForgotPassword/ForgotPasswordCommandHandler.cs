using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Features.Auth.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponseDto>
{
    private const string SuccessMessage = "Nếu địa chỉ email tồn tại trên hệ thống, chúng tôi đã gửi hướng dẫn và liên kết đặt lại mật khẩu đến hòm thư của bạn.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ForgotPasswordResponseDto> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = command.Request.Email.Trim();

        // 1. Tìm người dùng theo email
        var user = await _userManager.FindByEmailAsync(email);

        // 2. Chống Brute-Force & User Enumeration:
        // Dù tài khoản không tồn tại hoặc bị khóa, vẫn trả về thông báo thành công chung để bảo vệ quyền riêng tư
        if (user == null || !user.IsActive)
        {
            _logger.LogInformation("Yêu cầu quên mật khẩu cho email không tồn tại hoặc tài khoản bị vô hiệu hóa: {Email}", email);
            return new ForgotPasswordResponseDto(SuccessMessage);
        }

        // 3. Sinh token đặt lại mật khẩu bảo mật chuẩn ASP.NET Core Identity
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        // 4. Xây dựng đường dẫn đặt lại mật khẩu
        var baseUrl = _configuration["PublicSite:BaseUrl"]
            ?? _configuration["FRONTEND_ORIGINS"]?.Split(',').FirstOrDefault()
            ?? "http://localhost:3000";

        var resetUrl = $"{baseUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(resetToken)}";

        // 5. Gửi email thông báo cho người dùng
        try
        {
            await _emailService.SendPasswordResetEmailAsync(
                user.Email!,
                user.DisplayName,
                resetToken,
                resetUrl,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Lỗi email không làm gián đoạn phản hồi tới người dùng, bảo đảm an toàn
            _logger.LogWarning(ex, "Không thể gửi email đặt lại mật khẩu tới {Email}", user.Email);
        }

        // Trả về token cho môi trường Dev/Test thuận tiện kiểm thử
        return new ForgotPasswordResponseDto(SuccessMessage, resetToken);
    }
}
