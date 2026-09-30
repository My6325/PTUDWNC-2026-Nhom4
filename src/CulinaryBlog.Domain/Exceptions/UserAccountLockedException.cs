namespace CulinaryBlog.Domain.Exceptions;

/// <summary>
/// Tài khoản bị khóa do đăng nhập sai nhiều lần hoặc chưa kích hoạt (IsActive == false).
/// </summary>
public sealed class UserAccountLockedException(string message) : DomainException(message);
