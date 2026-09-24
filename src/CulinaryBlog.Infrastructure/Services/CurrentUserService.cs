using System.Security.Claims;
using CulinaryBlog.Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace CulinaryBlog.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) == true;
    }
}
