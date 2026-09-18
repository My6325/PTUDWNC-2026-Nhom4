namespace CulinaryBlog.Domain.Common;

/// <summary>
/// Lớp thực thể cơ sở (Base Entity) dùng chung cho toàn bộ các bảng trong CSDL Supabase PostgreSQL.
/// Cung cấp khóa chính GUID, kiểm soát dấu thời gian (Audit Timestamps),
/// cờ xóa mềm (Soft Delete 30 ngày) và Concurrency Token (xmin) của PostgreSQL.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Khóa chính định danh duy nhất toàn cầu (GUID/UUID).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Thời điểm tạo bản ghi (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Thời điểm cập nhật bản ghi gần nhất (UTC).
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Cờ đánh dấu trạng thái xóa mềm (Soft Delete).
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Thời điểm chuyển vào thùng rác xóa mềm (UTC) - phục vụ chu kỳ dọn rác 30 ngày của Hangfire.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Concurrency Token ánh xạ tới cột hệ thống xmin của PostgreSQL.
    /// Ngăn chặn lỗi ghi đè dữ liệu đồng thời (Lost Update / Optimistic Concurrency Conflict).
    /// </summary>
    public uint RowVersion { get; set; }
}
