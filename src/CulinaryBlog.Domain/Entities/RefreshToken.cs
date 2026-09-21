namespace CulinaryBlog.Domain.Entities;

/// <summary>
/// Thực thể Refresh Token dùng để cấp lại Access Token mà không yêu cầu người dùng đăng nhập lại.
/// Không kế thừa <see cref="CulinaryBlog.Domain.Common.BaseEntity"/> vì token không cần
/// cơ chế xóa mềm (Soft Delete) hay Concurrency Token (RowVersion).
/// Mỗi token chỉ được sử dụng một lần (Token Rotation) — khi dùng sẽ bị thu hồi
/// và thay thế bằng token mới (ghi nhận qua <see cref="ReplacedByTokenHash"/>).
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Khóa chính định danh duy nhất (GUID/UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Khóa ngoại tham chiếu tới <see cref="ApplicationUser.Id"/>.
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Chuỗi băm SHA-256 của Refresh Token gốc.
    /// Lưu hash thay vì token gốc để đảm bảo an toàn ngay cả khi CSDL bị rò rỉ.
    /// </summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>
    /// Thời điểm hết hạn của token (UTC).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Thời điểm tạo token (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Địa chỉ IP của client tại thời điểm tạo token.
    /// Có thể null nếu không xác định được IP.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Thời điểm token bị thu hồi (UTC).
    /// Giá trị null nghĩa là token chưa bị thu hồi.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Địa chỉ IP của client tại thời điểm thu hồi token.
    /// Có thể null nếu token chưa bị thu hồi hoặc không xác định được IP.
    /// </summary>
    public string? RevokedByIp { get; set; }

    /// <summary>
    /// Chuỗi băm SHA-256 của token thay thế (Token Rotation).
    /// Có giá trị khi token hiện tại đã bị thu hồi và được thay bằng token mới.
    /// </summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>
    /// Cho biết token có đang còn hiệu lực hay không.
    /// Token được coi là hợp lệ khi chưa bị thu hồi và chưa hết hạn.
    /// </summary>
    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
