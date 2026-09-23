using CulinaryBlog.Application.Features.Auth.Login;
using CulinaryBlog.Application.Features.Auth.Register;
using MediatR;

namespace CulinaryBlog.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IMediator mediator) =>
        {
            try
            {
                var command = new RegisterCommand(request);
                var response = await mediator.Send(command);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // TODO: sẽ thay bằng Exception Handling Middleware chuẩn ở bước sau
                return Results.Problem(ex.Message);
            }
        });

        group.MapPost("/login", async (LoginRequest request, IMediator mediator) =>
        {
            try
            {
                var command = new LoginCommand(request);
                var response = await mediator.Send(command);
                return Results.Ok(response);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // TODO: sẽ thay bằng Exception Handling Middleware chuẩn ở bước sau
                return Results.Problem(ex.Message);
            }
        });

        return app;
    }
}
