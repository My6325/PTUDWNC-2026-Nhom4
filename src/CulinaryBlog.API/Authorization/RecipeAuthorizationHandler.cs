using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace CulinaryBlog.API.Authorization;

public sealed class RecipeUpdateRequirement : IAuthorizationRequirement;

public sealed class RecipeAuthorizationHandler
    : AuthorizationHandler<RecipeUpdateRequirement, Recipe>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RecipeUpdateRequirement requirement,
        Recipe recipe)
    {
        var currentUserId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (context.User.IsInRole("Admin") ||
            string.Equals(recipe.AuthorId, currentUserId, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class RecipeAuthorizationService(
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor) : IRecipeAuthorizationService
{
    public async Task EnsureCanUpdateAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedException("Bạn cần đăng nhập để cập nhật công thức.");
        }

        var result = await authorizationService.AuthorizeAsync(
            user,
            recipe,
            new RecipeUpdateRequirement());

        if (!result.Succeeded)
        {
            throw new ForbiddenException("Bạn không có quyền cập nhật công thức này.");
        }
    }
}
