using CulinaryBlog.Application.Features.Recipes.Queries;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/recipes", async (
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
            .WithTags("Recipes")
            .CacheOutput("RecipesCache");

        return endpoints;
    }
}
