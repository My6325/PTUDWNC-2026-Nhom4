using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Application.Features.Recipes.CreateDraft;
using CulinaryBlog.Application.Features.Recipes.Delete;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Application.Features.Recipes.Images.DeleteImage;
using CulinaryBlog.Application.Features.Recipes.Images.UploadImage;
using CulinaryBlog.Application.Features.Recipes.Ingredients.AddIngredient;
using CulinaryBlog.Application.Features.Recipes.Ingredients.DeleteIngredient;
using CulinaryBlog.Application.Features.Recipes.Ingredients.UpdateIngredient;
using CulinaryBlog.Application.Features.Recipes.PublishRecipe;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Features.Recipes.Steps.AddStep;
using CulinaryBlog.Application.Features.Recipes.Steps.DeleteStep;
using CulinaryBlog.Application.Features.Recipes.Steps.UpdateStep;
using CulinaryBlog.Application.Features.Recipes.Update;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace CulinaryBlog.API.Endpoints;

public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/recipes").WithTags("Recipes");

        group.MapGet("/{slug}", async (string slug, ISender sender, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var detail = await sender.Send(new GetRecipeBySlugQuery(slug), cancellationToken);
                if (httpContext.Features.Get<Microsoft.AspNetCore.OutputCaching.IOutputCacheFeature>() is { } cacheFeature)
                {
                    cacheFeature.Context.Tags.Add("recipes");
                    cacheFeature.Context.Tags.Add($"recipe:{slug}");
                }
                return Results.Ok(detail);
            })
            .WithName("GetRecipeBySlug")
            .CacheOutput("RecipeDetail")
            .Produces<RecipeDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (
                [AsParameters] GetRecipesQuery query,
                ISender sender,
                CancellationToken cancellationToken) =>
            {
                try
                {
                    return (IResult)Results.Ok(await sender.Send(query, cancellationToken));
                }
                catch (ValidationException exception)
                {
                    var errors = exception.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).ToArray());

                    return Results.ValidationProblem(errors);
                }
            })
            .WithName("GetRecipes")
            .CacheOutput("RecipesCache");

        group.MapPost("/", CreateDraftAsync)
            .RequireAuthorization(policy => policy.RequireRole("Author", "Admin"))
            .WithName("CreateRecipeDraft")
            .WithSummary("Tạo bản nháp công thức món ăn mới")
            .Produces<RecipeDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization()
            .Produces<RecipeDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}/publish", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new PublishRecipeCommand(id), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("PublishRecipe")
            .WithSummary("Xuất bản công thức nấu ăn")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 1. DELETE /api/v1/recipes/{id:guid} (FR-RCP-007: Xóa công thức - Soft Delete)
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteRecipeCommand(id), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("DeleteRecipe")
            .WithSummary("Xóa công thức nấu ăn (Soft Delete)")
            .WithDescription("Xóa mềm công thức và các thực thể liên quan (bước nấu, nguyên liệu, hình ảnh), tự động vô hiệu hóa cache.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 2. POST /api/v1/recipes/{id:guid}/images (FR-RCP-008 & FR-FILE-001: Tải ảnh công thức lên Supabase Storage)
        group.MapPost("/{id:guid}/images", async (Guid id, IFormFile file, ISender sender, CancellationToken cancellationToken) =>
            {
                if (file is null || file.Length == 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["file"] = ["Vui lòng chọn tệp hình ảnh để tải lên."]
                    });
                }

                await using var stream = file.OpenReadStream();
                var image = await sender.Send(
                    new UploadRecipeImageCommand(id, stream, file.FileName, file.ContentType),
                    cancellationToken);

                return Results.Created($"/api/v1/recipes/{id}/images/{image.Id}", image);
            })
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithName("UploadRecipeImage")
            .WithSummary("Tải ảnh công thức lên Supabase Storage")
            .WithDescription("Thẩm duyệt Magic Bytes (JPEG/PNG/WebP), chống Path Traversal, tải lên bucket culinary-blog, tự động gán IsPrimary = true cho ảnh đầu tiên.")
            .Produces<RecipeImageResponseDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 3. DELETE /api/v1/recipes/{id:guid}/images/{imageId:guid} (FR-RCP-008 & FR-FILE-002: Xóa ảnh công thức)
        group.MapDelete("/{id:guid}/images/{imageId:guid}", async (Guid id, Guid imageId, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteRecipeImageCommand(id, imageId), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("DeleteRecipeImage")
            .WithSummary("Xóa ảnh công thức khỏi Supabase Storage")
            .WithDescription("Xóa tệp trên Supabase Storage; nếu xóa ảnh đại diện chính thì tự động chọn ảnh tiếp theo làm ảnh chính.")
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 4. POST /api/v1/recipes/{recipeId:guid}/steps (FR-RCP-010: Thêm bước làm mới)
        group.MapPost("/{recipeId:guid}/steps", async (Guid recipeId, AddRecipeStepRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var step = await sender.Send(new AddRecipeStepCommand(recipeId, request), cancellationToken);
                return Results.Created($"/api/v1/recipes/{recipeId}/steps/{step.Id}", step);
            })
            .RequireAuthorization()
            .WithName("AddRecipeStep")
            .WithSummary("Thêm bước làm mới cho công thức")
            .WithDescription("Tự động gán StepNumber = Steps.Count + 1.")
            .Produces<RecipeStepResponseDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 5. PUT /api/v1/recipes/{recipeId:guid}/steps/{stepId:guid} (FR-RCP-010: Cập nhật nội dung bước nấu ăn)
        group.MapPut("/{recipeId:guid}/steps/{stepId:guid}", async (Guid recipeId, Guid stepId, UpdateRecipeStepRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var step = await sender.Send(new UpdateRecipeStepCommand(recipeId, stepId, request), cancellationToken);
                return Results.Ok(step);
            })
            .RequireAuthorization()
            .WithName("UpdateRecipeStep")
            .WithSummary("Cập nhật nội dung bước nấu ăn")
            .WithDescription("Cập nhật Title, Description, TimerMinutes, ImageUrl cho bước làm.")
            .Produces<RecipeStepResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 6. DELETE /api/v1/recipes/{recipeId:guid}/steps/{stepId:guid} (FR-RCP-010: Xóa bước làm)
        group.MapDelete("/{recipeId:guid}/steps/{stepId:guid}", async (Guid recipeId, Guid stepId, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteRecipeStepCommand(recipeId, stepId), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("DeleteRecipeStep")
            .WithSummary("Xóa bước làm (Automatic Renumbering)")
            .WithDescription("Xóa bước nấu ăn và tự động đánh lại số thứ tự liên tục 1, 2, 3...")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // 7. POST /api/v1/recipes/{recipeId:guid}/ingredients (FR-RCP-009: Thêm nguyên liệu món ăn)
        group.MapPost("/{recipeId:guid}/ingredients", async (Guid recipeId, AddIngredientRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var ingredient = await sender.Send(new AddIngredientCommand(recipeId, request), cancellationToken);
                return Results.Created($"/api/v1/recipes/{recipeId}/ingredients/{ingredient.Id}", ingredient);
            })
            .RequireAuthorization()
            .WithName("AddRecipeIngredient")
            .WithSummary("Thêm nguyên liệu món ăn")
            .WithDescription("Thêm nguyên liệu vào danh sách nguyên liệu của công thức (tự động tăng OrderIndex).")
            .Produces<RecipeIngredientResponseDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 8. PUT /api/v1/recipes/{recipeId:guid}/ingredients/{ingredientId:guid} (FR-RCP-009: Sửa nguyên liệu món ăn)
        group.MapPut("/{recipeId:guid}/ingredients/{ingredientId:guid}", async (Guid recipeId, Guid ingredientId, UpdateIngredientRequest request, ISender sender, CancellationToken cancellationToken) =>
            {
                var ingredient = await sender.Send(new UpdateIngredientCommand(recipeId, ingredientId, request), cancellationToken);
                return Results.Ok(ingredient);
            })
            .RequireAuthorization()
            .WithName("UpdateRecipeIngredient")
            .WithSummary("Sửa nguyên liệu món ăn")
            .WithDescription("Cập nhật thông tin nguyên liệu: Name, Quantity, Unit, Notes.")
            .Produces<RecipeIngredientResponseDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 9. DELETE /api/v1/recipes/{recipeId:guid}/ingredients/{ingredientId:guid} (FR-RCP-009: Xóa nguyên liệu món ăn)
        group.MapDelete("/{recipeId:guid}/ingredients/{ingredientId:guid}", async (Guid recipeId, Guid ingredientId, ISender sender, CancellationToken cancellationToken) =>
            {
                await sender.Send(new DeleteIngredientCommand(recipeId, ingredientId), cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .WithName("DeleteRecipeIngredient")
            .WithSummary("Xóa nguyên liệu món ăn")
            .WithDescription("Xóa nguyên liệu khỏi công thức và tự động đánh lại số thứ tự OrderIndex.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateDraftAsync(
        CreateRecipeDraftRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var recipe = await sender.Send(new CreateRecipeDraftCommand(request), cancellationToken);
        return Results.Created($"/api/v1/recipes/{recipe.Id}", recipe);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var recipe = await sender.Send(new UpdateRecipeCommand(id, request), cancellationToken);
        return Results.Ok(recipe);
    }
}
