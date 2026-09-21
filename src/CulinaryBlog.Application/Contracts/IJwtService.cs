using System.Security.Claims;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.Application.Contracts;

/// <summary>
/// Hợp đồng dịch vụ JWT (JSON Web Token) phục vụ module xác thực (FR-AUTH).
/// Tầng Application chỉ định nghĩa interface — việc triển khai cụ thể nằm ở tầng Infrastructure.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Tạo Access Token (JWT) chứa thông tin định danh và phân quyền của người dùng.
    /// Token được ký bằng thuật toán HMAC-SHA256 với khóa bí mật từ cấu hình.
    /// </summary>
    /// <param name="user">Thực thể người dùng cần tạo token</param>
    /// <param name="roles">Danh sách vai trò (Role) của người dùng</param>
    /// <returns>Chuỗi JWT đã được ký, sẵn sàng trả về cho client</returns>
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);

    /// <summary>
    /// Sinh chuỗi Refresh Token ngẫu nhiên an toàn bằng bộ sinh số mật mã (CSPRNG).
    /// Kết quả là chuỗi 64 byte được mã hoá Base64Url, đảm bảo tính duy nhất và khó đoán.
    /// </summary>
    /// <returns>Chuỗi Refresh Token ngẫu nhiên dạng Base64Url</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Băm một chuỗi token bằng thuật toán SHA-256.
    /// Lưu hash thay vì token gốc trong CSDL để đảm bảo an toàn khi dữ liệu bị rò rỉ.
    /// </summary>
    /// <param name="token">Chuỗi token gốc cần băm</param>
    /// <returns>Chuỗi hash SHA-256 dạng Base64</returns>
    string HashToken(string token);

    /// <summary>
    /// Giải mã và xác minh chữ ký của Access Token đã hết hạn (ValidateLifetime = false).
    /// Phục vụ flow làm mới token (Refresh) — cho phép trích xuất claims từ token cũ.
    /// </summary>
    /// <param name="token">Chuỗi Access Token (JWT) đã hết hạn</param>
    /// <returns>
    /// <see cref="ClaimsPrincipal"/> chứa claims nếu token hợp lệ (đúng signature, đúng thuật toán HS256);
    /// <c>null</c> nếu token không hợp lệ (sai signature, sai thuật toán, bị giả mạo...).
    /// </returns>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
