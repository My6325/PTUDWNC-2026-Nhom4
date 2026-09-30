using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Auth.Login;
using CulinaryBlog.Application.Features.Auth.Register;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new RegisterCommand(request);
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("Register")
        .WithSummary("Register a new user account")
        .AllowAnonymous()
        .Produces<AuthResponseDto>(200)
        .ProducesProblem(409)
        .ProducesProblem(422);

        group.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request);
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("Login")
        .WithSummary("Log in and obtain access and refresh tokens")
        .AllowAnonymous()
        .Produces<AuthResponseDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(422)
        .ProducesProblem(423);

        return app;
    }
}
