using CulinaryBlog.Application.Contracts;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Services;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(IConfiguration configuration, ILogger<GoogleTokenValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken ct)
    {
        try
        {
            var clientId = _configuration["Google:ClientId"] ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
            
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrEmpty(clientId))
            {
                settings.Audience = new[] { clientId };
            }
            // If clientId is missing, we might still validate the signature but without audience checking,
            // or we could throw. Based on the requirement "đọc lười, thiếu cấu hình thì chỉ endpoint này lỗi, app vẫn khởi động được",
            // If it's strictly required, we should check it.
            // Let's configure it if it exists. Google API library requires Audience if we want to restrict it, 
            // but actually if we don't supply it, it will just validate the signature.
            // Wait, for security, if clientId is null, we should probably fail.
            if (string.IsNullOrEmpty(clientId))
            {
                _logger.LogWarning("Google ClientId is not configured. Google login will fail.");
                return null;
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            
            if (payload == null) return null;

            return new GoogleUserInfo(
                Subject: payload.Subject,
                Email: payload.Email,
                EmailVerified: payload.EmailVerified,
                Name: payload.Name,
                PictureUrl: payload.Picture
            );
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google JWT token.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token.");
            return null;
        }
    }
}
