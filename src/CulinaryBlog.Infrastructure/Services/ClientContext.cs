using CulinaryBlog.Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace CulinaryBlog.Infrastructure.Services;

public class ClientContext : IClientContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ip))
                {
                    return ip;
                }
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
