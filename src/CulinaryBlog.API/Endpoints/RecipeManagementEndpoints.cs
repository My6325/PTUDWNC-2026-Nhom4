using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Management;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class RecipeManagementEndpoints
{
    public static void MapRecipeManagement(this RouteGroupBuilder group)
    {
        group.MapGet("/mine", async (string? status, bool? trash, int? pageIndex, int? pageSize,
            ISender sender, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "private, no-store";
            return Results.Ok(await sender.Send(new MyRecipesQuery(status, trash ?? false, false, pageIndex ?? 1, pageSize ?? 12), ct));
        }).RequireAuthorization(policy => policy.RequireRole("Author", "Admin"));
        group.MapGet("/review-queue", async (int? pageIndex, int? pageSize, ISender sender, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "private, no-store";
            return Results.Ok(await sender.Send(new MyRecipesQuery(ReviewQueue: true, PageIndex: pageIndex ?? 1, PageSize: pageSize ?? 12), ct));
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        group.MapGet("/manage/{id:guid}", async (Guid id, ISender sender, HttpContext context, CancellationToken ct) =>
        {
            context.Response.Headers.CacheControl = "private, no-store";
            return Results.Ok(await sender.Send(new ManagedRecipeQuery(id), ct));
        }).RequireAuthorization();
        group.MapPut("/manage/{id:guid}", async (Guid id, SaveManagedRecipeRequest request, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new SaveManagedRecipeCommand(id, request), ct))).RequireAuthorization();

        foreach (var action in new[] { "submit", "withdraw", "publish", "archive", "unarchive", "unpublish", "restore", "delete" })
        {
            var methods = action == "delete" ? new[] { "DELETE" } : action == "publish" ? new[] { "PUT", "POST" } : new[] { "POST" };
            var route = action == "delete" ? "/{id:guid}" : $"/{{id:guid}}/{action}";
            var endpoint = group.MapMethods(route, methods, async (Guid id, ISender sender, HttpContext context, CancellationToken ct) =>
            {
                await sender.Send(new RecipeWorkflowCommand(id, action, ReadVersion(context)), ct);
                return Results.NoContent();
            }).RequireAuthorization();
            if (action == "publish") endpoint.RequireAuthorization(policy => policy.RequireRole("Admin"));
        }
        group.MapPost("/{id:guid}/reject", async (Guid id, ReviewDecisionRequest request, ISender sender, HttpContext context, CancellationToken ct) =>
        {
            await sender.Send(new RecipeWorkflowCommand(id, "reject", ReadVersion(context), request.Reason), ct);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }

    private static byte[] ReadVersion(HttpContext context)
    {
        try { return Convert.FromBase64String(context.Request.Headers.IfMatch.ToString().Trim('"')); }
        catch (FormatException) { throw new UnprocessableEntityException("ROW_VERSION_REQUIRED", "Gửi If-Match chứa RowVersion Base64."); }
    }
}
