namespace CulinaryBlog.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message)
{
    public virtual string Code => "DOMAIN_ERROR";
}
