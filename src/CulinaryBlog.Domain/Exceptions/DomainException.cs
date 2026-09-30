namespace CulinaryBlog.Domain.Exceptions;

/// <summary>
/// Lớp cơ sở trừu tượng cho tất cả các ngoại lệ nghiệp vụ thuộc tầng Domain.
/// </summary>
public abstract class DomainException(string message, string code = "DOMAIN_RULE_VIOLATION") : Exception(message)
{
    public string Code { get; } = code;
}
