using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Create;
using CulinaryBlog.Application.Features.Categories.Delete;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategoryBySlug;
using CulinaryBlog.Application.Features.Categories.Update;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CulinaryBlog.API.Endpoints;

/// <summary>
/// Đăng ký các endpoints Minimal APIs cho phân hệ Danh mục ẩm thực (FR-CAT).
/// </summary>
public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/categories")
            .WithTags("Categories");

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

        // 1. GET /api/v1/categories (FR-CAT-001: Lấy danh sách danh mục kèm cache)
        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var categories = await sender.Send(new GetCategoriesQuery(), cancellationToken);
            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithSummary("Lấy danh sách tất cả các danh mục ẩm thực")
        .WithDescription("Trả về danh sách 25 danh mục ẩm thực đã sắp xếp theo OrderIndex. Tích hợp In-Memory Cache (TTL 60 phút).")
        .Produces<List<CategoryDto>>(StatusCodes.Status200OK);

        // 2. GET /api/v1/categories/{slug} (FR-CAT-002: Lấy chi tiết danh mục kèm bài viết tóm tắt)
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
        .WithDescription("Tra cứu danh mục ẩm thực theo slug URL thân thiện. Kèm theo danh sách tóm tắt các món ăn Published thuộc danh mục này.")
        .Produces<CategoryDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // 3. PUT /api/v1/categories/{id:guid} (FR-CAT-004: Cập nhật thông tin danh mục [Admin])
        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var category = await sender.Send(new UpdateCategoryCommand(id, request), cancellationToken);
            return Results.Ok(category);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("UpdateCategory")
        .WithSummary("Cập nhật thông tin danh mục ẩm thực")
        .WithDescription("Cập nhật tên, mô tả, ảnh hoặc thứ tự hiển thị của danh mục (giữ nguyên slug). Yêu cầu quyền Admin.")
        .Produces<CategoryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 4. DELETE /api/v1/categories/{id:guid} (FR-CAT-005: Xóa danh mục ẩm thực [Admin])
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            await sender.Send(new DeleteCategoryCommand(id), cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("DeleteCategory")
        .WithSummary("Xóa danh mục ẩm thực")
        .WithDescription("Xóa mềm danh mục ẩm thực. Nếu còn công thức thuộc danh mục này, hệ thống sẽ trả về lỗi 409 Conflict.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}
