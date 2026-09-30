namespace CulinaryBlog.Domain.Exceptions;

/// <summary>
/// Lớp cơ sở trừu tượng cho tất cả các ngoại lệ nghiệp vụ thuộc tầng Domain.
/// Hỗ trợ cả 2 cách: truyền code qua constructor hoặc override thuộc tính Code.
/// </summary>
public abstract class DomainException : Exception
{
    public virtual string Code { get; }

    protected DomainException(string message, string code = "DOMAIN_RULE_VIOLATION") : base(message)
    {
        Code = code;
    }
}
