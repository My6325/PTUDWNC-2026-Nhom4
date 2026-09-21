using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Infrastructure.Services;

/// <summary>
/// Triển khai dịch vụ JWT phục vụ module xác thực (FR-AUTH).
/// Sử dụng thuật toán HMAC-SHA256 để ký Access Token và SHA-256 để băm Refresh Token.
/// Toàn bộ tham số cấu hình (Secret, Issuer, Audience, thời hạn) được đọc từ <see cref="JwtSettings"/>.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// Khởi tạo JwtService với cấu hình JWT được inject qua IOptions pattern.
    /// </summary>
    /// <param name="jwtOptions">Cấu hình JWT từ appsettings.json / biến môi trường</param>
    public JwtService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    /// <summary>
    /// Tạo Access Token (JWT) chứa thông tin định danh và phân quyền của người dùng.
    /// Token được ký bằng thuật toán HMAC-SHA256 với khóa bí mật từ cấu hình.
    /// Claims bao gồm: NameIdentifier (Id), Email, DisplayName, và danh sách Role.
    /// </summary>
    /// <param name="user">Thực thể người dùng cần tạo token</param>
    /// <param name="roles">Danh sách vai trò (Role) của người dùng</param>
    /// <returns>Chuỗi JWT đã được ký, sẵn sàng trả về cho client</returns>
    public string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        // 1. Tạo khóa đối xứng từ chuỗi Secret trong cấu hình
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

        // 2. Xây dựng danh sách claims cơ bản
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("DisplayName", user.DisplayName)
        };

        // 3. Bổ sung một claim "role" cho mỗi vai trò của người dùng
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // 4. Tạo JWT descriptor với Issuer, Audience, thời hạn từ cấu hình
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = signingCredentials
        };

        // 5. Tạo và serialize token thành chuỗi compact
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Sinh chuỗi Refresh Token ngẫu nhiên an toàn bằng bộ sinh số mật mã (CSPRNG).
    /// Sử dụng <see cref="RandomNumberGenerator"/> thay vì <see cref="Random"/> để đảm bảo tính bảo mật.
    /// Kết quả là chuỗi 64 byte được mã hoá Base64Url, đảm bảo tính duy nhất và khó đoán.
    /// </summary>
    /// <returns>Chuỗi Refresh Token ngẫu nhiên dạng Base64Url</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Băm một chuỗi token bằng thuật toán SHA-256.
    /// Lưu hash thay vì token gốc trong CSDL để đảm bảo an toàn khi dữ liệu bị rò rỉ.
    /// </summary>
    /// <param name="token">Chuỗi token gốc cần băm</param>
    /// <returns>Chuỗi hash SHA-256 dạng Base64</returns>
    public string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Giải mã và xác minh chữ ký của Access Token đã hết hạn (ValidateLifetime = false).
    /// Phục vụ flow làm mới token (Refresh) — cho phép trích xuất claims từ token cũ.
    /// Chỉ chấp nhận token ký bằng thuật toán HMAC-SHA256 với đúng Issuer/Audience.
    /// </summary>
    /// <param name="token">Chuỗi Access Token (JWT) đã hết hạn</param>
    /// <returns>
    /// <see cref="ClaimsPrincipal"/> chứa claims nếu token hợp lệ;
    /// <c>null</c> nếu token không hợp lệ (sai signature, sai thuật toán, bị giả mạo).
    /// </returns>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false, // Cho phép token đã hết hạn
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            // Chỉ chấp nhận thuật toán HMAC-SHA256 để ngăn chặn tấn công Algorithm Confusion
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256Signature]
        };

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            // Kiểm tra thêm: token phải là JwtSecurityToken và sử dụng đúng thuật toán HS256
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            // Token không hợp lệ (sai signature, bị giả mạo, format sai...)
            return null;
        }
    }
}
