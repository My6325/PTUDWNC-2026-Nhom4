using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Application.Features.Recipes.CreateDraft;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Features.Recipes.Update;
using FluentValidation;
using MediatR;

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
