using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Auth.Login;
using CulinaryBlog.Application.Features.Auth.Register;
using CulinaryBlog.Application.Features.Auth.Refresh;
using CulinaryBlog.Application.Features.Auth.Logout;
using CulinaryBlog.Application.Features.Auth.Google;
using CulinaryBlog.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        group.MapPost("/google", async (GoogleLoginRequest request, HttpContext context, ISender sender, CancellationToken cancellationToken) =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var command = new GoogleLoginCommand(request.IdToken, clientIp, userAgent);
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("GoogleLogin")
        .WithSummary("Log in using Google OAuth 2.0")
        .AllowAnonymous()
        .Produces<AuthResponseDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(403)
        .ProducesProblem(422);

        group.MapPost("/refresh", async (HttpRequest httpRequest, HttpResponse httpResponse, IOptions<JwtSettings> jwtOptions, IWebHostEnvironment env, ISender sender, CancellationToken cancellationToken) =>
        {
            string? token = null;
            if (httpRequest.HasJsonContentType())
            {
                try
                {
                    var body = await httpRequest.ReadFromJsonAsync<RefreshTokenRequest>(cancellationToken);
                    token = body?.RefreshToken;
                }
                catch { }
            }
            if (string.IsNullOrEmpty(token))
            {
                token = httpRequest.Cookies["refreshToken"];
            }

            var command = new RefreshTokenCommand(new RefreshTokenRequest(token));
            var response = await sender.Send(command, cancellationToken);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth",
                Secure = !env.IsDevelopment(),
                Expires = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays)
            };
            httpResponse.Cookies.Append("refreshToken", response.RefreshToken, cookieOptions);

            return Results.Ok(response);
        })
        .WithName("RefreshToken")
        .WithSummary("Refresh access and refresh tokens")
        .AllowAnonymous()
        .Produces<AuthResponseDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(422);

        group.MapPost("/logout", async (HttpRequest httpRequest, HttpResponse httpResponse, ISender sender, CancellationToken cancellationToken) =>
        {
            string? token = null;
            if (httpRequest.HasJsonContentType())
            {
                try
                {
                    var body = await httpRequest.ReadFromJsonAsync<RefreshTokenRequest>(cancellationToken);
                    token = body?.RefreshToken;
                }
                catch { }
            }
            if (string.IsNullOrEmpty(token))
            {
                token = httpRequest.Cookies["refreshToken"];
            }

            var command = new LogoutCommand(new RefreshTokenRequest(token));
            await sender.Send(command, cancellationToken);

            httpResponse.Cookies.Delete("refreshToken", new CookieOptions { Path = "/api/v1/auth" });
            return Results.NoContent();
        })
        .WithName("Logout")
        .WithSummary("Log out and revoke refresh token")
        .RequireAuthorization()
        .Produces(204)
        .ProducesProblem(401)
        .ProducesProblem(422);

        group.MapGet("/me", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var query = new CulinaryBlog.Application.Features.Auth.Me.GetCurrentUserProfileQuery();
            var response = await sender.Send(query, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("GetCurrentUser")
        .WithSummary("Get the current user's profile")
        .RequireAuthorization()
        .Produces<UserDto>(200)
        .ProducesProblem(401);

        group.MapPatch("/profile", async (CulinaryBlog.Application.Features.Auth.Profile.UpdateProfileRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var command = new CulinaryBlog.Application.Features.Auth.Profile.UpdateProfileCommand(request);
            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("UpdateProfile")
        .WithSummary("Update the current user's profile")
        .RequireAuthorization()
        .Produces<UserDto>(200)
        .ProducesProblem(401)
        .ProducesProblem(422);

        return app;
    }
}
