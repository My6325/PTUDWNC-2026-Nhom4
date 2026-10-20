using Asp.Versioning;
using Asp.Versioning.Builder;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Create;
using CulinaryBlog.Application.Features.Categories.Delete;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryBySlug;
using CulinaryBlog.Application.Features.Categories.Queries.GetTrashCategories;
using CulinaryBlog.Application.Features.Categories.Restore;
using CulinaryBlog.Application.Features.Categories.Update;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CulinaryBlog.API.Endpoints;

/// <summary>
/// Đăng ký các endpoints Minimal APIs cho phân hệ Danh mục ẩm thực (FR-CAT).
/// Hỗ trợ đầy đủ nghiệp vụ CRUD và Thùng rác (Soft Delete Trash & Restore).
/// </summary>
public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app, ApiVersionSet? versionSet = null)
    {
        var group = versionSet is not null
            ? app.MapGroup("/api/v{version:apiVersion}/categories").WithApiVersionSet(versionSet).MapToApiVersion(new ApiVersion(1, 0))
            : app.MapGroup("/api/v1/categories");

        group.WithTags("Categories");

        // 1. POST /api/v1/categories (FR-CAT-003: Tạo mới danh mục [Admin])
        group.MapPost("/", async (CreateCategoryRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var category = await sender.Send(new CreateCategoryCommand(request), cancellationToken);
                return Results.Created($"/api/v1/categories/{category.Slug}", category);
            })
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithName("CreateCategory")
            .WithSummary("Tạo mới danh mục ẩm thực")
            .Produces<CategoryDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 2. GET /api/v1/categories (FR-CAT-001: Lấy danh sách danh mục đang hoạt động)
        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var categories = await sender.Send(new GetCategoriesQuery(), cancellationToken);
            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithSummary("Lấy danh sách tất cả các danh mục ẩm thực đang hoạt động")
        .WithDescription("Trả về danh sách danh mục ẩm thực đã sắp xếp theo OrderIndex. Tích hợp In-Memory Cache (TTL 60 phút).")
        .Produces<List<CategoryDto>>(StatusCodes.Status200OK);

        // 3. GET /api/v1/categories/trash (Lấy danh sách danh mục trong Thùng rác [Admin])
        group.MapGet("/trash", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var trashCategories = await sender.Send(new GetTrashCategoriesQuery(), cancellationToken);
            return Results.Ok(trashCategories);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetTrashCategories")
        .WithSummary("Lấy danh sách các danh mục trong Thùng rác [Admin]")
        .WithDescription("Lấy danh sách các danh mục đã bị xóa mềm, lưu vết tối đa 30 ngày trước khi xóa vĩnh viễn.")
        .Produces<List<CategoryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // 4. GET /api/v1/categories/{slug} (FR-CAT-002: Lấy chi tiết danh mục kèm bài viết tóm tắt)
        group.MapGet("/{slug}", async (string slug, ISender sender, CancellationToken cancellationToken) =>
        {
            var categoryDetail = await sender.Send(new GetCategoryBySlugQuery(slug), cancellationToken);
            if (categoryDetail is null)
            {
                return Results.NotFound(new
                {
                    status = 404,
                    title = "Not Found",
                    detail = $"Không tìm thấy danh mục ẩm thực với slug '{slug}'"
                });
            }

            return Results.Ok(categoryDetail);
        })
        .WithName("GetCategoryBySlug")
        .WithSummary("Lấy chi tiết danh mục và danh sách công thức thuộc danh mục theo slug")
        .Produces<CategoryDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // 5. PUT /api/v1/categories/{id:guid} (FR-CAT-004: Cập nhật thông tin danh mục [Admin])
        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var category = await sender.Send(new UpdateCategoryCommand(id, request), cancellationToken);
            return Results.Ok(category);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("UpdateCategory")
        .WithSummary("Cập nhật thông tin danh mục ẩm thực")
        .Produces<CategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 6. DELETE /api/v1/categories/{id:guid} (FR-CAT-005: Xóa mềm danh mục ẩm thực [Admin])
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteCategoryCommand(id), cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("DeleteCategory")
        .WithSummary("Xóa mềm danh mục ẩm thực chuyển vào Thùng rác")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // 7. POST /api/v1/categories/{id:guid}/restore (Khôi phục danh mục từ Thùng rác [Admin])
        group.MapPost("/{id:guid}/restore", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var category = await sender.Send(new RestoreCategoryCommand(id), cancellationToken);
            return Results.Ok(category);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("RestoreCategory")
        .WithSummary("Khôi phục danh mục ẩm thực từ Thùng rác về trạng thái hoạt động")
        .Produces<CategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
