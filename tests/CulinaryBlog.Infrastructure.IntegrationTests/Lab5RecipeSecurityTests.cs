using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CulinaryBlog.API.Authorization;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.API.ExceptionHandling;
using CulinaryBlog.API.OpenApi;
using CulinaryBlog.API.Security;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Contracts;
using CulinaryBlog.Application.Features.Recipes.Common;
using CulinaryBlog.Application.Features.Recipes.GetBySlug;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Xunit;

namespace CulinaryBlog.Infrastructure.IntegrationTests;

public sealed class Lab5RecipeSecurityTests
{
    [Theory]
    [InlineData("owner", "Author", true)]
    [InlineData("other", "Admin", true)]
    [InlineData("other", "Author", false)]
    public async Task Handler_ShouldAuthorizeOwnerOrAdmin(string userId, string role, bool allowed)
    {
        var requirement = new RecipeUpdateRequirement();
        var context = new AuthorizationHandlerContext([requirement], User(userId, role), new Recipe { AuthorId = "owner" });
        await new RecipeAuthorizationHandler().HandleAsync(context);
        Assert.Equal(allowed, context.HasSucceeded);
    }

    [Theory]
    [InlineData(null, null, HttpStatusCode.Unauthorized)]
    [InlineData("other", "Author", HttpStatusCode.Forbidden)]
    [InlineData("owner", "Author", HttpStatusCode.OK)]
    [InlineData("other", "Admin", HttpStatusCode.OK)]
    public async Task UpdateRecipe_ShouldEnforceJwtAndOwnership(string? userId, string? role, HttpStatusCode expected)
    {
        await using var backend = await LocalBackend.StartAsync();
        if (userId is not null)
            backend.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(userId, role!));

        var request = new UpdateRecipeRequest("Updated recipe", "Description", null, null, 10, 20, 2,
            RecipeDifficulty.Easy, null, RecipeMappings.ToBytes(backend.Repository.Recipe.RowVersion));
        using var response = await backend.Client.PutAsJsonAsync($"/api/v1/recipes/{backend.Repository.Recipe.Id}", request);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(expected == HttpStatusCode.OK ? 1 : 0, backend.Repository.SaveCount);
        if (expected == HttpStatusCode.OK)
        {
            var dto = await response.Content.ReadFromJsonAsync<RecipeDto>();
            Assert.Equal("Updated recipe", dto!.Title);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.String, json.RootElement.GetProperty("rowVersion").ValueKind);
            Assert.Equal(1, json.RootElement.GetProperty("difficulty").GetInt32());
        }
    }

    [Fact]
    public async Task InvalidJwt_ShouldReturn401()
    {
        await using var backend = await LocalBackend.StartAsync();
        backend.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        using var response = await backend.Client.DeleteAsync($"/api/v1/recipes/{backend.Repository.Recipe.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("http://localhost:3000", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task Preflight_ShouldOnlyAllowConfiguredOrigin(string origin, bool allowed)
    {
        await using var backend = await LocalBackend.StartAsync();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/recipes");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        using var response = await backend.Client.SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
        {
            Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
            Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        }
    }

    [Fact]
    public async Task RateLimiter_ShouldReturn429After60Requests()
    {
        await using var backend = await LocalBackend.StartAsync();
        for (var i = 0; i < 60; i++)
        {
            using var response = await backend.Client.GetAsync("/probe");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        // Fill the two queue slots concurrently; the next request must reject immediately.
        var queued1 = backend.Client.GetAsync("/probe");
        var queued2 = backend.Client.GetAsync("/probe");
        var overflow = backend.Client.GetAsync("/probe");
        var rejected = await Task.WhenAny(queued1, queued2, overflow);
        using var response429 = await rejected;
        Assert.Equal(HttpStatusCode.TooManyRequests, response429.StatusCode);
        using var problem = JsonDocument.Parse(await response429.Content.ReadAsStringAsync());
        Assert.Equal(429, problem.RootElement.GetProperty("status").GetInt32());
        backend.Client.CancelPendingRequests();
        foreach (var pending in new[] { queued1, queued2, overflow }.Where(t => t != rejected))
        {
            try { using var response = await pending; }
            catch (OperationCanceledException) { }
        }
    }

    [Fact]
    public async Task Documentation_ShouldExposeRecipeSchemasAndScalar()
    {
        await using var backend = await LocalBackend.StartAsync();
        using var response = await backend.Client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var bearer = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
        var update = paths.GetProperty("/api/v1/recipes/{id}").GetProperty("put");
        Assert.Equal("UpdateRecipe", update.GetProperty("operationId").GetString());
        foreach (var status in new[] { "200", "401", "403", "404", "409", "422" })
            Assert.True(update.GetProperty("responses").TryGetProperty(status, out _));
        var upload = paths.GetProperty("/api/v1/recipes/{id}/images").GetProperty("post");
        Assert.True(upload.GetProperty("requestBody").GetProperty("content").TryGetProperty("multipart/form-data", out _));
        Assert.True(upload.GetProperty("responses").TryGetProperty("400", out _));
        using var scalar = await backend.Client.GetAsync("/scalar/v1");
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
        Assert.Equal("text/html", scalar.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<html>", await scalar.Content.ReadAsStringAsync());
    }

    private static ClaimsPrincipal User(string id, string role) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role)], "test"));

    private const string SigningKey = "lab5-local-test-signing-key-with-at-least-32-bytes";
    private static string Token(string id, string role) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        issuer: "lab5-tests", audience: "lab5-tests", claims: User(id, role).Claims,
        expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256)));

    private sealed class LocalBackend(WebApplication app, HttpClient client, MemoryRecipes repository) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;
        public MemoryRecipes Repository { get; } = repository;
        public static async Task<LocalBackend> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            { ApplicationName = typeof(RecipeEndpoints).Assembly.FullName, EnvironmentName = "Testing" });
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddApplication();
            builder.Services.AddFrontendRequestSecurity();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
                    ValidIssuer = "lab5-tests", ValidAudience = "lab5-tests",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                    RoleClaimType = ClaimTypes.Role, NameClaimType = ClaimTypes.NameIdentifier,
                });
            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<IAuthorizationHandler, RecipeAuthorizationHandler>();
            builder.Services.AddScoped<IRecipeAuthorizationService, RecipeAuthorizationService>();
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddCulinaryOpenApi();
            var repository = new MemoryRecipes();
            builder.Services.AddSingleton<IRecipeRepository>(repository);
            builder.Services.AddSingleton<IUnitOfWork>(repository);
            builder.Services.AddSingleton<IRecipeCacheInvalidator, NoCache>();
            var app = builder.Build();
            app.UseExceptionHandler();
            app.UseCors("FrontendPolicy");
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapRecipeEndpoints();
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.MapGet("/probe", () => "ok");
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            return new LocalBackend(app, new HttpClient { BaseAddress = new Uri(address) }, repository);
        }
        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private sealed class MemoryRecipes : IRecipeRepository, IUnitOfWork
    {
        public Recipe Recipe { get; } = Recipe.Create("Test recipe", "test-recipe", "Description", null, null, "owner", 10, 20, 2, RecipeDifficulty.Easy);
        public int SaveCount { get; private set; }
        public IRecipeRepository Recipes => this;
        public ICategoryRepository Categories => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken ct) { SaveCount++; return Task.FromResult(1); }
        public Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Recipe?>(id == Recipe.Id ? Recipe : null);
        public Task<bool> SlugExistsAsync(string slug, Guid? excluded, CancellationToken ct) => Task.FromResult(false);
        public Task<Recipe?> GetByIdForPublishingAsync(Guid id, CancellationToken ct) => GetByIdAsync(id, ct);
        public Task AddAsync(Recipe recipe, CancellationToken ct) => throw new NotSupportedException();
        public Task<RecipeDetailDto?> GetDetailBySlugAsync(string slug, CancellationToken ct) => throw new NotSupportedException();
        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(string? search, Guid? category, string? difficulty, string sort, int page, int size, CancellationToken ct) => throw new NotSupportedException();
        public Task<PaginatedResult<RecipeListDto>> SearchRecipesAsync(string? search, int page, int size, CancellationToken ct) => throw new NotSupportedException();
    }
    private sealed class NoCache : IRecipeCacheInvalidator
    {
        public Task InvalidateListAsync(CancellationToken ct) => Task.CompletedTask;
        public Task InvalidateDetailAsync(string slug, CancellationToken ct) => Task.CompletedTask;
    }
}
